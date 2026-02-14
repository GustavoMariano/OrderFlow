namespace OrderFlow.Application.Abstractions.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct) where T : class;
}
