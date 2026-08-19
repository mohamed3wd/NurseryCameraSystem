using System.IO.Compression;
using System.Text.Json;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using NurseryCamera.Api;
using NurseryCamera.Api.Hubs;
using NurseryCamera.Api.Middleware;
using NurseryCamera.Application.Common.Models;
using NurseryCamera.Application.Common.Options;
using NurseryCamera.Application.DependencyInjection;
using NurseryCamera.Infrastructure.DependencyInjection;
using NurseryCamera.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---- Services -------------------------------------------------------------

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

// JSON list responses (children, cameras, audit pages) compress extremely well and the parent
// app is expected to run on mobile networks. HTTPS-only compression of JSON is safe here because
// no secret is ever reflected back from a request body into a response.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "NurseryCamera API", Version = "v1" });

    const string bearerSchemeId = "Bearer";

    options.AddSecurityDefinition(bearerSchemeId, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a JWT access token (from POST /api/auth/login)."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(bearerSchemeId, document)] = []
    });
});

builder.Services.AddSignalR();

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
const string CorsPolicyName = "NurseryCameraCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddNurseryCameraAuthorization();

var apiErrorSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

// Keep unauthenticated/forbidden responses in the same ApiError envelope as every other
// error (spec section 26), rather than the bare empty 401/403 ASP.NET Core returns by default.
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.Events ??= new JwtBearerEvents();

    options.Events.OnChallenge = context =>
    {
        context.HandleResponse();
        return WriteApiErrorAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
            "AUTHENTICATION_REQUIRED", "Authentication is required.");
    };

    options.Events.OnForbidden = context =>
        WriteApiErrorAsync(context.HttpContext, StatusCodes.Status403Forbidden,
            "FORBIDDEN", "You are not authorized to perform this action.");
});

Task WriteApiErrorAsync(HttpContext httpContext, int statusCode, string code, string message)
{
    httpContext.Response.StatusCode = statusCode;
    httpContext.Response.ContentType = "application/json";

    var traceId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value) && value is string correlationId
        ? correlationId
        : httpContext.TraceIdentifier;

    var json = JsonSerializer.Serialize(new ApiError(code, message, traceId), apiErrorSerializerOptions);

    return httpContext.Response.WriteAsync(json);
}

var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(sqlConnectionString))
{
    healthChecksBuilder.AddSqlServer(sqlConnectionString, name: "sql", tags: new[] { "ready" });
}

var redisConnectionString = builder.Configuration.GetSection(RedisOptions.SectionName)["ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    healthChecksBuilder.AddRedis(redisConnectionString, name: "redis", tags: new[] { "ready" });
}

// AspNetCoreRateLimit: IP-based throttling configured via the IpRateLimiting section
// (see appsettings.json). Protects auth/login and viewing-session endpoints from abuse.
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();

// ---- Pipeline ---------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Correlation id must be established before exception handling so error responses
// (and logs) can carry a stable trace id back to the caller.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseResponseCompression();

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NurseryHub>("/hubs/nursery");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

if (app.Environment.IsDevelopment())
{
    await DbSeeder.SeedAsync(app.Services);
}

app.Run();

/// <summary>Exposed for WebApplicationFactory-based integration tests.</summary>
public partial class Program
{
}
