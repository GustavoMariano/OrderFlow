namespace OrderFlow.Contracts.Orders.V1;

public sealed record OrderCompletedV1(Guid OrderId, Guid UserId);
