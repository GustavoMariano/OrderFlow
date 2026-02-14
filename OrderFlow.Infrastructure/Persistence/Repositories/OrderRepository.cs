using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence.Postgres;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderFlowDbContext _db;

    public OrderRepository(OrderFlowDbContext db) => _db = db;

    public Task AddAsync(Order order, CancellationToken ct)
        => _db.Orders.AddAsync(order, ct).AsTask();

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct)
        => _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct);

    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        => await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(ct);

    public Task UpdateAsync(Order order, CancellationToken ct)
    {
        _db.Orders.Update(order);
        return Task.CompletedTask;
    }
}
