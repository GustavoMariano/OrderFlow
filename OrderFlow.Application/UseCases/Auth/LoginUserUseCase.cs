using OrderFlow.Application.Abstractions.Persistence;
using OrderFlow.Application.Abstractions.Security;
using OrderFlow.Application.Common;
using OrderFlow.Application.DTOs.Auth;

namespace OrderFlow.Application.UseCases.Auth;

public sealed class LoginUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenService _tokenService;

    public LoginUserUseCase(IUserRepository users, IPasswordHasher passwordHasher, IAccessTokenService tokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> ExecuteAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<AuthResponse>.Failure("auth.invalid_credentials", "Invalid credentials.");

        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null)
            return Result<AuthResponse>.Failure("auth.invalid_credentials", "Invalid credentials.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure("auth.invalid_credentials", "Invalid credentials.");

        var (token, expiresAtUtc) = _tokenService.CreateToken(user);
        return Result<AuthResponse>.Success(new AuthResponse(token, expiresAtUtc));
    }
}
