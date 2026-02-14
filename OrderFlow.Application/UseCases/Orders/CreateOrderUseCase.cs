using OrderFlow.Application.Abstractions.Messaging;
using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Application.Common;
using OrderFlow.Application.DTOs.Orders;
using OrderFlow.Contracts.Abstractions;
using OrderFlow.Contracts.Orders.V1;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.UseCases.Orders;

public sealed class CreateOrderUseCase
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly IEventPublisher _publisher;

    public CreateOrderUseCase(IOrderRepository orders, IUnitOfWork uow, IEventPublisher publisher)
    {
        _orders = orders;
        _uow = uow;
        _publisher = publisher;
    }

    public async Task<Result<Guid>> ExecuteAsync(Guid userId, CreateOrderRequest request, Guid correlationId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return Result<Guid>.Failure("orders.invalid_user", "UserId is required.");

        if (request.Items is null || request.Items.Count == 0)
            return Result<Guid>.Failure("orders.empty_items", "Order must have at least one item.");

        var order = new Order(userId, request.Currency);

        foreach (var item in request.Items)
            order.AddItem(item.Sku, item.Name, item.Quantity, item.UnitPrice);

        await _orders.AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);

        var envelope = EventEnvelope.Create(
            correlationId: correlationId,
            eventType: "OrderCreated.v1",
            data: new OrderCreatedV1(order.Id, order.UserId, order.Currency, order.TotalAmount));

        await _publisher.PublishAsync(envelope, ct);

        return Result<Guid>.Success(order.Id);
    }
}
