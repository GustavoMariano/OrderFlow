using Microsoft.Extensions.Options;
using OrderFlow.Application.Abstractions.Orders;
using OrderFlow.Contracts.Abstractions;
using OrderFlow.Contracts.Orders.V1;
using OrderFlow.Infrastructure.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections;
using System.Text;
using System.Text.Json;

namespace OrderFlow.Worker.Consumers;

public sealed class OrderCreatedConsumer : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string QueueName = "orderflow.orders.created.v1";
    private const string RetryQueueName = "orderflow.orders.created.v1.retry";
    private const string DlqQueueName = "orderflow.orders.created.v1.dlq";

    private const string RoutingKey = "orders.created.v1";
    private const string RetryRoutingKey = "orders.created.v1.retry";
    private const string DlqRoutingKey = "orders.created.v1.dlq";

    private const int MaxRetries = 5;
    private const int RetryDelayMs = 5000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public OrderCreatedConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderCreatedConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _connection = await _factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(options: null, cancellationToken: ct);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await _channel.QueueDeclareAsync(
            queue: DlqQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: ct);

        await _channel.QueueBindAsync(
            queue: DlqQueueName,
            exchange: _options.Exchange,
            routingKey: DlqRoutingKey,
            arguments: null,
            cancellationToken: ct);

        var retryArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _options.Exchange,
            ["x-dead-letter-routing-key"] = RoutingKey,
            ["x-message-ttl"] = RetryDelayMs
        };

        await _channel.QueueDeclareAsync(
            queue: RetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryArgs,
            cancellationToken: ct);

        await _channel.QueueBindAsync(
            queue: RetryQueueName,
            exchange: _options.Exchange,
            routingKey: RetryRoutingKey,
            arguments: null,
            cancellationToken: ct);

        var mainArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = _options.Exchange,
            ["x-dead-letter-routing-key"] = RetryRoutingKey
        };

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: mainArgs,
            cancellationToken: ct);

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: _options.Exchange,
            routingKey: RoutingKey,
            arguments: null,
            cancellationToken: ct);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageAsync;

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: ct);

        _logger.LogInformation(
            "OrderCreatedConsumer started. Exchange={Exchange} Queue={Queue} RetryQueue={RetryQueue} DlqQueue={DlqQueue} MaxRetries={MaxRetries} RetryDelayMs={RetryDelayMs}",
            _options.Exchange, QueueName, RetryQueueName, DlqQueueName, MaxRetries, RetryDelayMs);
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs e)
    {
        var deliveryTag = e.DeliveryTag;
        var redelivered = e.Redelivered;

        string body = "";
        Guid correlationId = Guid.Empty;
        Guid orderId = Guid.Empty;

        try
        {
            body = Encoding.UTF8.GetString(e.Body.ToArray());

            var envelope = JsonSerializer.Deserialize<EventEnvelope<OrderCreatedV1>>(body, JsonOptions);

            if (envelope is null)
            {
                _logger.LogWarning(
                    "Invalid envelope (null). DeliveryTag={DeliveryTag} Redelivered={Redelivered}",
                    deliveryTag, redelivered);

                await PublishToDlqAsync(e, body, reason: "invalid_envelope");
                await _channel!.BasicAckAsync(deliveryTag, multiple: false);
                return;
            }

            correlationId = envelope.CorrelationId;
            orderId = envelope.Data.OrderId;

            var attempts = GetRetryCount(e);

            _logger.LogInformation(
                "Received OrderCreated. CorrelationId={CorrelationId} OrderId={OrderId} Attempts={Attempts}/{MaxRetries} DeliveryTag={DeliveryTag} Redelivered={Redelivered}",
                correlationId, orderId, attempts, MaxRetries, deliveryTag, redelivered);

            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IOrderProcessor>();

            await processor.ProcessAsync(correlationId, orderId, CancellationToken.None);

            await _channel!.BasicAckAsync(deliveryTag, multiple: false);

            _logger.LogInformation(
                "Processed OrderCreated OK. CorrelationId={CorrelationId} OrderId={OrderId}",
                correlationId, orderId);
        }
        catch (Exception ex)
        {
            var attempts = GetRetryCount(e);

            _logger.LogError(
                ex,
                "Failed processing OrderCreated. CorrelationId={CorrelationId} OrderId={OrderId} Attempts={Attempts}/{MaxRetries} DeliveryTag={DeliveryTag} Redelivered={Redelivered}",
                correlationId, orderId, attempts, MaxRetries, deliveryTag, redelivered);

            if (attempts >= MaxRetries)
            {
                await PublishToDlqAsync(e, body, reason: "max_retries_exceeded");
                await _channel!.BasicAckAsync(deliveryTag, multiple: false);
                return;
            }

            await _channel!.BasicRejectAsync(deliveryTag, requeue: false);
        }
    }

    private static int GetRetryCount(BasicDeliverEventArgs e)
    {
        if (e.BasicProperties?.Headers is null)
            return 0;

        if (!e.BasicProperties.Headers.TryGetValue("x-death", out var xDeathObj))
            return 0;

        try
        {
            if (xDeathObj is not IList list)
                return 0;

            var total = 0;

            foreach (var item in list)
            {
                if (item is not IDictionary<string, object> entry)
                    continue;

                if (entry.TryGetValue("count", out var countObj) && countObj is long count)
                    total += (int)count;
            }

            return total;
        }
        catch
        {
            return 0;
        }
    }

    private async Task PublishToDlqAsync(BasicDeliverEventArgs e, string body, string reason)
    {
        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        props.Headers = new Dictionary<string, object>
        {
            ["x-error-reason"] = reason,
            ["x-original-routing-key"] = e.RoutingKey ?? "",
            ["x-original-exchange"] = e.Exchange ?? ""
        };

        var bytes = Encoding.UTF8.GetBytes(body);

        await _channel!.BasicPublishAsync(
            exchange: _options.Exchange,
            routingKey: DlqRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: bytes);

        _logger.LogWarning(
            "Message sent to DLQ. Reason={Reason} Exchange={Exchange} RoutingKey={RoutingKey}",
            reason, _options.Exchange, DlqRoutingKey);
    }

    public async ValueTask DisposeAsync()
    {
        try { if (_channel is not null) await _channel.CloseAsync(); } catch { }
        try { if (_connection is not null) await _connection.CloseAsync(); } catch { }
    }
}
