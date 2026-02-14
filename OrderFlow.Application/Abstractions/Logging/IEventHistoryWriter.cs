namespace OrderFlow.Application.Abstractions.Logging;

public interface IEventHistoryWriter
{
    Task AppendAsync(EventHistoryEntry entry, CancellationToken ct);
}

public sealed record EventHistoryEntry(
    Guid CorrelationId,
    Guid EventId,
    string EventType,
    DateTimeOffset OccurredAtUtc,
    object Data);
