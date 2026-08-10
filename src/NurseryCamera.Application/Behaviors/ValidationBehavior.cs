using FluentValidation;
using MediatR;
using NurseryCamera.Application.Common.Exceptions;

namespace NurseryCamera.Application.Behaviors;

/// <summary>
/// Runs all registered FluentValidation validators for the request before it reaches its
/// handler. Failures short-circuit the pipeline as an <see cref="AppException"/> with code
/// VALIDATION_ERROR, so callers/tests never need to special-case FluentValidation's own
/// exception type.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
                .SelectMany(result => result.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count > 0)
            {
                var errors = failures
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

                throw AppException.ValidationFailed(errors);
            }
        }

        return await next(cancellationToken);
    }
}
