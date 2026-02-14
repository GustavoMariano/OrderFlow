using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Abstractions.Security;

public interface IAccessTokenService
{
    (string token, DateTimeOffset expiresAtUtc) CreateToken(User user);
}
