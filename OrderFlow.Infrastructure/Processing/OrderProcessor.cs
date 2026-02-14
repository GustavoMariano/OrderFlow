using OrderFlow.Application.Abstractions.Logging;
using OrderFlow.Application.Abstractions.Messaging;
using OrderFlow.Application.Abstractions.Orders;
using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Contracts.Abstractions;
using OrderFlow.Contracts.Orders.V1;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Infrastructure.Processing;

public sealed class OrderProcessor : IOrderProcessor
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly IProcessingLogWriter _log;
    private readonly IEventHistoryWriter _history;
    private readonly IEventPublisher _publisher;

    public OrderProcessor(
        IOrderRepository orders,
        IUnitOfWork uow,
        IProcessingLogWriter log,
        IEventHistoryWriter history,
        IEventPublisher publisher)
    {
        _orders = orders;
        _uow = uow;
        _log = log;
        _history = history;
        _publisher = publisher;
    }

    public async Task ProcessAsync(Guid correlationId, Guid orderId, CancellationToken ct)
    {
        await _log.WriteAsync(new(
            correlationId, orderId,
            Step: "worker.start",
            Message: "Starting order processing",
            Level: "Information",
            OccurredAtUtc: DateTimeOffset.UtcNow), ct);

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
        {
            await _log.WriteAsync(new(
                correlationId, orderId,
                Step: "worker.not_found",
                Message: "Order not found in database",
                Level: "Warning",
                OccurredAtUtc: DateTimeOffset.UtcNow), ct);
            return;
        }

        try
        {
            order.MarkProcessing();
            await _orders.UpdateAsync(order, ct);
            await _uow.SaveChangesAsync(ct);

            await _log.WriteAsync(new(
                correlationId, orderId,
                Step: "worker.processing",
                Message: "Order marked as Processing",
                Level: "Information",
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Data: new { order.Status, order.TotalAmount }), ct);

            await Task.Delay(TimeSpan.FromSeconds(2), ct);

            order.MarkCompleted();
            await _orders.UpdateAsync(order, ct);
            await _uow.SaveChangesAsync(ct);

            var completed = EventEnvelope.Create(
                correlationId: correlationId,
                eventType: "OrderCompleted.v1",
                data: new OrderCompletedV1(order.Id, order.UserId));

            await _history.AppendAsync(new(
                correlationId,
                completed.EventId,
                completed.EventType,
                completed.OccurredAtUtc,
                completed), ct);

            await _publisher.PublishAsync(completed, ct);

            await _log.WriteAsync(new(
                correlationId, orderId,
                Step: "worker.completed",
                Message: "Order completed successfully",
                Level: "Information",
                OccurredAtUtc: DateTimeOffset.UtcNow), ct);
        }
        catch (Exception ex)
        {
            try
            {
                if (order.Status != OrderStatus.Completed)
                {
                    order.MarkFailed();
                    await _orders.UpdateAsync(order, ct);
                    await _uow.SaveChangesAsync(ct);
                }
            }
            catch
            {
            }

            var failed = EventEnvelope.Create(
                correlationId: correlationId,
                eventType: "OrderFailed.v1",
                data: new OrderFailedV1(order.Id, order.UserId, ex.Message));

            await _history.AppendAsync(new(
                correlationId,
                failed.EventId,
                failed.EventType,
                failed.OccurredAtUtc,
                failed), ct);

            await _publisher.PublishAsync(failed, ct);

            await _log.WriteAsync(new(
                correlationId, orderId,
                Step: "worker.failed",
                Message: "Order processing failed",
                Level: "Error",
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Exception: ex.ToString()), ct);
        }
    }
}
