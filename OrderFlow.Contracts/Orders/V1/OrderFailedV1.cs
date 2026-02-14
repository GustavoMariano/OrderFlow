namespace OrderFlow.Contracts.Orders.V1;

public sealed record OrderFailedV1(Guid OrderId, Guid UserId, string Reason);
