using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Application.Common;
using OrderFlow.Application.DTOs.Orders;

namespace OrderFlow.Application.UseCases.Orders;

public sealed class GetOrderByIdUseCase
{
    private readonly IOrderRepository _orders;

    public GetOrderByIdUseCase(IOrderRepository orders) => _orders = orders;

    public async Task<Result<OrderResponse>> ExecuteAsync(Guid userId, Guid orderId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return Result<OrderResponse>.Failure("orders.invalid_user", "UserId is required.");

        if (orderId == Guid.Empty)
            return Result<OrderResponse>.Failure("orders.invalid_order", "OrderId is required.");

        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null || order.UserId != userId)
            return Result<OrderResponse>.Failure("orders.not_found", "Order not found.");

        var mapped = new OrderResponse(
            order.Id,
            order.UserId,
            order.Status,
            order.Currency,
            order.TotalAmount,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.Items.Select(i => new OrderItemResponse(i.Id, i.Sku, i.Name, i.Quantity, i.UnitPrice, i.LineTotal)).ToList()
        );

        return Result<OrderResponse>.Success(mapped);
    }
}
