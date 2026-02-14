using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Application.Abstractions.Security;
using OrderFlow.Application.Common;
using OrderFlow.Application.DTOs.Auth;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.UseCases.Auth;

public sealed class RegisterUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _uow;

    public RegisterUserUseCase(IUserRepository users, IPasswordHasher passwordHasher, IUnitOfWork uow)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _uow = uow;
    }

    public async Task<Result<Guid>> ExecuteAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(email))
            return Result<Guid>.Failure("auth.invalid_email", "Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return Result<Guid>.Failure("auth.weak_password", "Password must be at least 8 characters.");

        var existing = await _users.GetByEmailAsync(email, ct);
        if (existing is not null)
            return Result<Guid>.Failure("auth.email_in_use", "Email is already in use.");

        var hash = _passwordHasher.Hash(request.Password);

        var user = new User(email, hash);
        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<Guid>.Success(user.Id);
    }
}
