using RabbitMQ.Client.Exceptions;

namespace OrderFlow.Worker.HostedServices;

public sealed class RabbitMqConsumerHostedService : BackgroundService
{
    private readonly Consumers.OrderCreatedConsumer _consumer;
    private readonly ILogger<RabbitMqConsumerHostedService> _logger;

    public RabbitMqConsumerHostedService(
        Consumers.OrderCreatedConsumer consumer,
        ILogger<RabbitMqConsumerHostedService> logger)
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
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown normal
                return;
            }
            catch (BrokerUnreachableException ex)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(30, 2 * attempt));
                _logger.LogWarning(ex, "RabbitMQ unreachable. Retrying in {DelaySeconds}s...", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
            catch (ConnectFailureException ex)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(30, 2 * attempt));
                _logger.LogWarning(ex, "RabbitMQ connection failed. Retrying in {DelaySeconds}s...", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                var delay = TimeSpan.FromSeconds(10);
                _logger.LogError(ex, "Unexpected error starting consumer. Retrying in {DelaySeconds}s...", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}
