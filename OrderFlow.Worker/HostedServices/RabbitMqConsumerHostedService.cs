using RabbitMQ.Client.Exceptions;

namespace OrderFlow.Worker.HostedServices;

public sealed class RabbitMqConsumerHostedService : BackgroundService
{
    private readonly Consumers.OrderCreatedConsumer _consumer;
    private readonly ILogger<RabbitMqConsumerHostedService> _logger;

    public RabbitMqConsumerHostedService(Consumers.OrderCreatedConsumer consumer, ILogger<RabbitMqConsumerHostedService> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                attempt++;

                _logger.LogInformation("Starting RabbitMQ consumer (attempt {Attempt})...", attempt);

                await _consumer.StartAsync(stoppingToken);

                _logger.LogInformation("RabbitMQ consumer started successfully.");

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (BrokerUnreachableException ex)
            {
                await DisposeConsumerSilentlyAsync();

                var delay = GetBackoffDelay(attempt);
                _logger.LogWarning(
                    ex,
                    "RabbitMQ unreachable. Retrying in {DelaySeconds}s (attempt {Attempt})...",
                    delay.TotalSeconds,
                    attempt);

                await Task.Delay(delay, stoppingToken);
            }
            catch (ConnectFailureException ex)
            {
                await DisposeConsumerSilentlyAsync();

                var delay = GetBackoffDelay(attempt);
                _logger.LogWarning(
                    ex,
                    "RabbitMQ connection failed. Retrying in {DelaySeconds}s (attempt {Attempt})...",
                    delay.TotalSeconds,
                    attempt);

                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                await DisposeConsumerSilentlyAsync();

                var delay = TimeSpan.FromSeconds(10);
                _logger.LogError(
                    ex,
                    "Unexpected error starting consumer. Retrying in {DelaySeconds}s (attempt {Attempt})...",
                    delay.TotalSeconds,
                    attempt);

                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private static TimeSpan GetBackoffDelay(int attempt)
    {
        var baseSeconds = Math.Min(30, 2 * attempt);
        var jitterSeconds = Random.Shared.NextDouble();
        return TimeSpan.FromSeconds(baseSeconds + jitterSeconds);
    }

    private async Task DisposeConsumerSilentlyAsync()
    {
        try
        {
            await _consumer.DisposeAsync();
        }
        catch
        {
        }
    }
}
