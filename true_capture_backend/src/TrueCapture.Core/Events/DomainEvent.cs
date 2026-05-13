namespace TrueCapture.Core.Events;

public abstract record DomainEvent(DateTime OccurredAtUtc)
{
    protected DomainEvent() : this(DateTime.UtcNow) { }
}
