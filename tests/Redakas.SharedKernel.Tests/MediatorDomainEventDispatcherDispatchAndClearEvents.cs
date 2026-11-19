using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Redakas.SharedKernel.Tests;

/// <summary>
/// Domain events are dispatched after the change that raised them is saved, so dispatch is
/// best-effort: every event is published and cleared, and a failing handler is logged without
/// failing the caller. Cancellation still stops it.
/// </summary>
public class MediatorDomainEventDispatcherDispatchAndClearEvents
{
    private sealed record Happened(string What) : DomainEventBase;

    private sealed class Thing : HasDomainEventsBase
    {
        public void Raise(string what) => RegisterDomainEvent(new Happened(what));
    }

    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly FakeLogger<MediatorDomainEventDispatcher> _logger = new();

    private static IDomainEvent Published(string what) =>
        Arg.Is<IDomainEvent>(e => e.GetType() == typeof(Happened) && ((Happened)e).What == what);

    private Task Dispatch(params Thing[] things) =>
        new MediatorDomainEventDispatcher(_mediator, _logger).DispatchAndClearEvents(things);

    [Fact]
    public async Task PublishesEveryEventAndClearsThem()
    {
        var thing = new Thing();
        thing.Raise("first");
        thing.Raise("second");

        await Dispatch(thing);

        await _mediator.Received(1).Publish(Published("first"), Arg.Any<CancellationToken>());
        await _mediator.Received(1).Publish(Published("second"), Arg.Any<CancellationToken>());
        thing.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task LogsAFailingHandlerAndPublishesTheRest()
    {
        var thing = new Thing();
        thing.Raise("fails");
        thing.Raise("works");
        _mediator
            .Publish(Published("fails"), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException(new InvalidOperationException("boom")));

        await Dispatch(thing);

        await _mediator.Received(1).Publish(Published("works"), Arg.Any<CancellationToken>());
        _logger.LatestRecord.Level.ShouldBe(LogLevel.Error);
        _logger.LatestRecord.Exception.ShouldBeOfType<InvalidOperationException>();
        thing.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task LetsCancellationThrough()
    {
        var thing = new Thing();
        thing.Raise("cancelled");
        _mediator
            .Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException(new OperationCanceledException()));

        await Should.ThrowAsync<OperationCanceledException>(() => Dispatch(thing));
    }
}
