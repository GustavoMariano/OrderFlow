using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence.Postgres;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly OrderFlowDbContext _db;

    public UserRepository(OrderFlowDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        => _db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken ct)
        => _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);

    public Task AddAsync(User user, CancellationToken ct)
        => _db.Users.AddAsync(user, ct).AsTask();
}
