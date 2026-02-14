using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Infrastructure.Persistence.Postgres;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly OrderFlowDbContext _db;

    public UnitOfWork(OrderFlowDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}
