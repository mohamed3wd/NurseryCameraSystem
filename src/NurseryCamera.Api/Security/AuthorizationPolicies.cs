using Microsoft.AspNetCore.Authorization;

namespace NurseryCamera.Api;

/// <summary>
/// Central catalog of authorization policy names used across controllers, mapped onto the
/// three ASP.NET Identity roles seeded by <c>DbSeeder</c> (Admin/Staff/Parent). Admin is
/// treated as a superuser for every admin-facing policy; Staff additionally gets
/// <see cref="AttendanceManager"/> so front-desk staff can check children in/out.
/// </summary>
public static class AuthorizationPolicies
{
    public const string ParentOnly = nameof(ParentOnly);
    public const string StaffOnly = nameof(StaffOnly);
    public const string NurseryAdmin = nameof(NurseryAdmin);
    public const string AttendanceManager = nameof(AttendanceManager);
    public const string CameraManager = nameof(CameraManager);
    public const string AuditViewer = nameof(AuditViewer);

    private const string AdminRole = "Admin";
    private const string StaffRole = "Staff";
    private const string ParentRole = "Parent";

    public static void AddNurseryCameraAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(ParentOnly, policy => policy.RequireRole(ParentRole))
            .AddPolicy(StaffOnly, policy => policy.RequireRole(StaffRole))
            .AddPolicy(NurseryAdmin, policy => policy.RequireRole(AdminRole))
            .AddPolicy(AttendanceManager, policy => policy.RequireRole(StaffRole, AdminRole))
            .AddPolicy(CameraManager, policy => policy.RequireRole(AdminRole))
            .AddPolicy(AuditViewer, policy => policy.RequireRole(AdminRole));
    }
}
