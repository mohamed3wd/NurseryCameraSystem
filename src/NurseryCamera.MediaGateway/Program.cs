using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("api", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiBase = config["NurseryApi:BaseUrl"] ?? "http://localhost:8080";
    client.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
    var apiKey = config["NurseryApi:MediaGatewayApiKey"] ?? config["MediaGateway:ApiKey"];
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.Remove("X-Media-Gateway-Key");
        client.DefaultRequestHeaders.Add("X-Media-Gateway-Key", apiKey);
    }
});

builder.Services.AddHttpClient("go2rtc", (sp, client) =>
{
    var go2rtc = sp.GetRequiredService<IConfiguration>()["Go2Rtc:BaseUrl"] ?? "http://localhost:1984";
    client.BaseAddress = new Uri(go2rtc.TrimEnd('/') + "/");
});

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ??
[
    "http://localhost:4200",
    "http://localhost:4300"
];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

/// <summary>
/// Browser → Media Gateway WebRTC signaling. Token is validated against the API;
/// RTSP is resolved privately and never returned to the browser.
/// </summary>
app.MapPost("/viewer/webrtc", async (
    HttpRequest httpRequest,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("WebRtcViewer");

    if (!Guid.TryParse(httpRequest.Query["sessionId"], out var sessionId))
    {
        return Results.BadRequest(new { code = "VALIDATION_ERROR", message = "sessionId is required." });
    }

    ViewerOfferRequest? body;
    try
    {
        body = await httpRequest.ReadFromJsonAsync<ViewerOfferRequest>(cancellationToken);
    }
    catch
    {
        return Results.BadRequest(new { code = "VALIDATION_ERROR", message = "Invalid JSON body." });
    }

    if (body is null || string.IsNullOrWhiteSpace(body.StreamToken) || string.IsNullOrWhiteSpace(body.Sdp))
    {
        return Results.BadRequest(new { code = "VALIDATION_ERROR", message = "streamToken and sdp are required." });
    }

    var api = httpClientFactory.CreateClient("api");
    var resolveResponse = await api.PostAsJsonAsync(
        "api/internal/stream/resolve",
        new { viewingSessionId = sessionId, streamToken = body.StreamToken },
        cancellationToken);

    if (!resolveResponse.IsSuccessStatusCode)
    {
        logger.LogWarning("Stream resolve failed with status {StatusCode}.", (int)resolveResponse.StatusCode);
        return Results.Json(
            new { code = "STREAM_AUTHORIZATION_FAILED", message = "Unable to authorize the stream." },
            statusCode: StatusCodes.Status502BadGateway);
    }

    var resolved = await resolveResponse.Content.ReadFromJsonAsync<StreamResolveDto>(cancellationToken: cancellationToken);
    if (resolved is null || !resolved.Authorized || string.IsNullOrWhiteSpace(resolved.StreamName) || string.IsNullOrWhiteSpace(resolved.SourceUrl))
    {
        return Results.Json(
            new
            {
                code = resolved?.DenialCode ?? "STREAM_AUTHORIZATION_FAILED",
                message = resolved?.DenialMessage ?? "Stream authorization failed."
            },
            statusCode: StatusCodes.Status403Forbidden);
    }

    var go2rtc = httpClientFactory.CreateClient("go2rtc");

    // Register / refresh the private stream on go2rtc (internal network only).
    var putUrl =
        $"api/streams?name={Uri.EscapeDataString(resolved.StreamName)}&src={Uri.EscapeDataString(resolved.SourceUrl)}";
    using (var put = await go2rtc.PutAsync(putUrl, content: null, cancellationToken))
    {
        if (!put.IsSuccessStatusCode)
        {
            logger.LogWarning("go2rtc stream registration returned {StatusCode}.", (int)put.StatusCode);
        }
    }

    using var sdpContent = new StringContent(body.Sdp, Encoding.UTF8);
    sdpContent.Headers.ContentType = new MediaTypeHeaderValue("application/sdp");

    using var webrtcResponse = await go2rtc.PostAsync(
        $"api/webrtc?src={Uri.EscapeDataString(resolved.StreamName)}",
        sdpContent,
        cancellationToken);

    if (!webrtcResponse.IsSuccessStatusCode)
    {
        var err = await webrtcResponse.Content.ReadAsStringAsync(cancellationToken);
        logger.LogWarning("go2rtc WebRTC negotiation failed: {Error}", err);
        return Results.Json(
            new { code = "STREAM_NEGOTIATION_FAILED", message = "Unable to negotiate WebRTC with the media server." },
            statusCode: StatusCodes.Status502BadGateway);
    }

    var answerSdp = await webrtcResponse.Content.ReadAsStringAsync(cancellationToken);
    return Results.Json(new ViewerAnswerResponse(answerSdp, "webrtc"));
});

app.Run();

internal sealed record ViewerOfferRequest(
    [property: JsonPropertyName("streamToken")] string StreamToken,
    [property: JsonPropertyName("sdp")] string Sdp);

internal sealed record ViewerAnswerResponse(
    [property: JsonPropertyName("sdp")] string Sdp,
    [property: JsonPropertyName("mediaProtocol")] string MediaProtocol);

internal sealed record StreamResolveDto(
    [property: JsonPropertyName("authorized")] bool Authorized,
    [property: JsonPropertyName("streamName")] string? StreamName,
    [property: JsonPropertyName("sourceUrl")] string? SourceUrl,
    [property: JsonPropertyName("denialCode")] string? DenialCode,
    [property: JsonPropertyName("denialMessage")] string? DenialMessage);
