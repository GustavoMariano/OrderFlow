namespace OrderFlow.Application.Abstractions.Orders;

public interface IOrderProcessor
{
    Task ProcessAsync(Guid correlationId, Guid orderId, CancellationToken ct);
}
