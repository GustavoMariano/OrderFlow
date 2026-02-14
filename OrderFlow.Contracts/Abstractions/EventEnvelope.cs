namespace OrderFlow.Contracts.Abstractions;

public sealed record EventEnvelope<T>(
    Guid EventId,
    Guid CorrelationId,
    DateTimeOffset OccurredAtUtc,
    string EventType,
    T Data) where T : class;

public static class EventEnvelope
{
    public static EventEnvelope<T> Create<T>(Guid correlationId, string eventType, T data)
        where T : class
        => new(
            EventId: Guid.NewGuid(),
            CorrelationId: correlationId == Guid.Empty ? Guid.NewGuid() : correlationId,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            EventType: eventType,
            Data: data);
}
