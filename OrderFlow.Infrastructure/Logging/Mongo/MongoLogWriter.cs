using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using OrderFlow.Application.Abstractions.Logging;
using OrderFlow.Infrastructure.Options;

namespace OrderFlow.Infrastructure.Logging.Mongo;

public sealed class MongoLogWriter : IProcessingLogWriter, IEventHistoryWriter
{
    private readonly IMongoCollection<BsonDocument> _processingLogs;
    private readonly IMongoCollection<BsonDocument> _eventHistory;

    public MongoLogWriter(IOptions<MongoOptions> options)
    {
        var opt = options.Value;
        var client = new MongoClient(opt.ConnectionString);
        var db = client.GetDatabase(opt.Database);

        _processingLogs = db.GetCollection<BsonDocument>("processing_logs");
        _eventHistory = db.GetCollection<BsonDocument>("event_history");
    }

    public Task WriteAsync(ProcessingLogEntry entry, CancellationToken ct)
    {
        var doc = new BsonDocument
        {
            ["correlationId"] = entry.CorrelationId.ToString(),
            ["orderId"] = entry.OrderId.ToString(),
            ["step"] = entry.Step,
            ["message"] = entry.Message,
            ["level"] = entry.Level,
            ["occurredAtUtc"] = entry.OccurredAtUtc.UtcDateTime,
            ["data"] = entry.Data is null ? BsonNull.Value : entry.Data.ToBsonDocument(),
            ["exception"] = entry.Exception is null ? BsonNull.Value : entry.Exception
        };

        return _processingLogs.InsertOneAsync(doc, cancellationToken: ct);
    }

    public Task AppendAsync(EventHistoryEntry entry, CancellationToken ct)
    {
        var doc = new BsonDocument
        {
            ["correlationId"] = entry.CorrelationId.ToString(),
            ["eventId"] = entry.EventId.ToString(),
            ["eventType"] = entry.EventType,
            ["occurredAtUtc"] = entry.OccurredAtUtc.UtcDateTime,
            ["data"] = entry.Data.ToBsonDocument()
        };

        return _eventHistory.InsertOneAsync(doc, cancellationToken: ct);
    }
}
