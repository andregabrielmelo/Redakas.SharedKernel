namespace Redakas.SharedKernel;

/// <summary>
/// Publishes domain events through Mediator. Best-effort: by the time events are dispatched the
/// change that raised them is saved, so a failing handler is logged and never fails the caller
/// (a 500 for a write that happened invites the client to repeat it). Work that must happen
/// reliably needs a transactional outbox instead. It logs through <c>ILogger</c>, since the
/// shared kernel sits below every application layer and can't see their logging abstractions.
/// </summary>
public partial class MediatorDomainEventDispatcher(
    IMediator mediator,
    ILogger<MediatorDomainEventDispatcher> logger
) : IDomainEventDispatcher
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<MediatorDomainEventDispatcher> _logger = logger;

    public async Task DispatchAndClearEvents(IEnumerable<IHasDomainEvents> entitiesWithEvents)
    {
        foreach (var entity in entitiesWithEvents)
        {
            IDomainEvent[] events = entity.DomainEvents.ToArray();
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                try
                {
                    await _mediator.Publish(domainEvent).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    LogHandlerFailed(_logger, exception, domainEvent.GetType().Name);
                }
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "A handler of {DomainEvent} failed after the changes were saved; the changes stay saved"
    )]
    private static partial void LogHandlerFailed(
        ILogger logger,
        Exception exception,
        string domainEvent
    );
}
