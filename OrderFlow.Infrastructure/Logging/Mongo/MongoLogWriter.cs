using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using OrderFlow.Application.Abstractions.Logging;
using OrderFlow.Infrastructure.Options;

namespace OrderFlow.Infrastructure.Logging.Mongo;

public sealed class MongoLogWriter : IProcessingLogWriter, IEventHistoryWriter
{
    private readonly IMongoCollection<BsonDocument> _processingLogs;
    private readonly IMongoCollection<BsonDocument> _eventHistory;
    private readonly ILogger<MongoLogWriter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public MongoLogWriter(IOptions<MongoOptions> options, ILogger<MongoLogWriter> logger)
    {
        _logger = logger;

        var opt = options.Value;
        var client = new MongoClient(opt.ConnectionString);
        var db = client.GetDatabase(opt.Database);

        _processingLogs = db.GetCollection<BsonDocument>("processing_logs");
        _eventHistory = db.GetCollection<BsonDocument>("event_history");
    }

    public async Task WriteAsync(ProcessingLogEntry entry, CancellationToken ct)
    {
        try
        {
            var doc = new BsonDocument
            {
                ["correlationId"] = entry.CorrelationId.ToString(),
                ["orderId"] = entry.OrderId.ToString(),
                ["step"] = entry.Step,
                ["message"] = entry.Message,
                ["level"] = entry.Level,
                ["occurredAtUtc"] = entry.OccurredAtUtc.UtcDateTime,
                ["data"] = ToSafeBsonValue(entry.Data),
                ["exception"] = entry.Exception is null ? BsonNull.Value : entry.Exception
            };

            await _processingLogs.InsertOneAsync(doc, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed writing processing log to MongoDB. CorrelationId={CorrelationId} OrderId={OrderId} Step={Step}",
                entry.CorrelationId, entry.OrderId, entry.Step);
        }
    }

    public async Task AppendAsync(EventHistoryEntry entry, CancellationToken ct)
    {
        try
        {
            var doc = new BsonDocument
            {
                ["correlationId"] = entry.CorrelationId.ToString(),
                ["eventId"] = entry.EventId.ToString(),
                ["eventType"] = entry.EventType,
                ["occurredAtUtc"] = entry.OccurredAtUtc.UtcDateTime,
                ["data"] = ToSafeBsonValue(entry.Data)
            };

            await _eventHistory.InsertOneAsync(doc, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed writing event history to MongoDB. CorrelationId={CorrelationId} EventId={EventId} EventType={EventType}",
                entry.CorrelationId, entry.EventId, entry.EventType);
        }
    }

    private static BsonValue ToSafeBsonValue(object? data)
    {
        if (data is null)
            return BsonNull.Value;

        if (data is BsonValue bsonValue)
            return bsonValue;

        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);

            if (string.IsNullOrWhiteSpace(json))
                return BsonNull.Value;

            if (json.StartsWith("{"))
                return BsonDocument.Parse(json);

            if (json.StartsWith("["))
                return BsonArray.Create(JsonSerializer.Deserialize<List<object>>(json, JsonOptions) ?? new List<object>());

            return new BsonString(json);
        }
        catch
        {
            return new BsonString(data.ToString() ?? string.Empty);
        }
    }
}
