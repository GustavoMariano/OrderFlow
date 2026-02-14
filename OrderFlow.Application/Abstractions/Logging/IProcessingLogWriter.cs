namespace OrderFlow.Application.Abstractions.Logging;

public interface IProcessingLogWriter
{
    Task WriteAsync(ProcessingLogEntry entry, CancellationToken ct);
}

public sealed record ProcessingLogEntry(
    Guid CorrelationId,
    Guid OrderId,
    string Step,
    string Message,
    string Level,
    DateTimeOffset OccurredAtUtc,
    object? Data = null,
    string? Exception = null);
