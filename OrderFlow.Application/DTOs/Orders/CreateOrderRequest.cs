namespace OrderFlow.Application.DTOs.Orders;

public sealed record CreateOrderRequest(string Currency, IReadOnlyList<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(string Sku, string Name, int Quantity, decimal UnitPrice);
