using MediatR;
using NurseryCamera.Application.Abstractions.Persistence;
using NurseryCamera.Application.Common.Exceptions;
using NurseryCamera.Domain.Exceptions;

namespace NurseryCamera.Application.Behaviors;

/// <summary>
/// Commits the unit of work exactly once per request. Handlers, <c>IAuditService</c> and
/// <c>INotificationService</c> only stage entity changes, so an audit entry or outbox message
/// costs no extra database round trip: everything a request touches is flushed in one batch,
/// inside the implicit SaveChanges transaction.
///
/// Expected business failures still commit, because handlers deliberately stage denial audit
/// records and terminal session state immediately before throwing (spec section 25). Unexpected
/// exceptions discard the staged changes, since the tracked graph cannot be trusted at that point.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAppDbContext _db;

    public UnitOfWorkBehavior(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        TResponse response;

        try
        {
            response = await next(cancellationToken);
        }
        catch (Exception ex) when (ex is AppException or DomainException)
        {
            // The caller is aborting on purpose; the denial audit trail must survive it.
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return response;
    }
}
