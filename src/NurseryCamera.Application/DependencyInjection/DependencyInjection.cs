using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NurseryCamera.Application.Behaviors;
using NurseryCamera.Application.Features.Cameras.Policies;

namespace NurseryCamera.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Registered last so it sits closest to the handler: only work the handler actually
        // staged gets committed, and validation failures never reach a SaveChanges.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        services.AddScoped<ICameraAccessPolicy, CameraAccessPolicy>();

        return services;
    }
}
