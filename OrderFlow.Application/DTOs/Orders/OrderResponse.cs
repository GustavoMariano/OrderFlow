using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.DTOs.Orders;

public sealed record OrderItemResponse(Guid Id, string Sku, string Name, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record OrderResponse(
    Guid Id,
    Guid UserId,
    OrderStatus Status,
    string Currency,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items);
