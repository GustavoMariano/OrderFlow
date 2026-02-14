using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Application.Common;
using OrderFlow.Application.DTOs.Orders;

namespace OrderFlow.Application.UseCases.Orders;

public sealed class GetOrdersUseCase
{
    private readonly IOrderRepository _orders;

    public GetOrdersUseCase(IOrderRepository orders) => _orders = orders;

    public async Task<Result<IReadOnlyList<OrderResponse>>> ExecuteAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return Result<IReadOnlyList<OrderResponse>>.Failure("orders.invalid_user", "UserId is required.");

        var orders = await _orders.GetByUserIdAsync(userId, ct);

        var mapped = orders.Select(o => new OrderResponse(
            o.Id,
            o.UserId,
            o.Status,
            o.Currency,
            o.TotalAmount,
            o.CreatedAtUtc,
            o.UpdatedAtUtc,
            o.Items.Select(i => new OrderItemResponse(i.Id, i.Sku, i.Name, i.Quantity, i.UnitPrice, i.LineTotal)).ToList()
        )).ToList();

        return Result<IReadOnlyList<OrderResponse>>.Success(mapped);
    }
}
