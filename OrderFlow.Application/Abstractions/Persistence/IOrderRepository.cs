using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct);

    Task UpdateAsync(Order order, CancellationToken ct);
}
