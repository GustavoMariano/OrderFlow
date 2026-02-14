using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using OrderFlow.Application.Abstractions.Messaging;
using OrderFlow.Infrastructure.Options;

namespace OrderFlow.Infrastructure.Messaging.RabbitMQ;

public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ConnectionFactory _factory;

    private IConnection? _connection;
    private IChannel? _channel;

    private readonly SemaphoreSlim _initLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;

        _factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };
    }

    public async Task PublishAsync<T>(T message, CancellationToken ct) where T : class
    {
        ct.ThrowIfCancellationRequested();

        await EnsureInitializedAsync(ct);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));

        var props = new BasicProperties
        {
            Persistent = true
        };

        var routingKey = ResolveRoutingKey(message);

        await _channel!.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_channel is not null && _connection is not null)
            return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_channel is not null && _connection is not null)
                return;

            _connection = await _factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(options: null, cancellationToken: ct);

            await _channel.ExchangeDeclareAsync(
                exchange: _options.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static string ResolveRoutingKey<T>(T message)
    {
        var eventTypeProp = message!.GetType().GetProperty("EventType");
        var eventType = eventTypeProp?.GetValue(message)?.ToString() ?? "unknown";

        return eventType switch
        {
            "OrderCreated.v1" => "orders.created.v1",
            "OrderCompleted.v1" => "orders.completed.v1",
            "OrderFailed.v1" => "orders.failed.v1",
            _ => "orders.unknown"
        };
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_channel is not null)
                await _channel.CloseAsync();

            if (_connection is not null)
                await _connection.CloseAsync();
        }
        catch
        {
        }
        finally
        {
            _channel = null;
            _connection = null;
            _initLock.Dispose();
        }
    }
}
