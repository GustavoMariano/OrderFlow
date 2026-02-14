namespace OrderFlow.Contracts.Orders.V1;

public sealed record OrderCreatedV1(
    Guid OrderId,
    Guid UserId,
    string Currency,
    decimal TotalAmount);
