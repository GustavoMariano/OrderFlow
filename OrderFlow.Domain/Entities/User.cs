using OrderFlow.Domain.Common;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public sealed class User : AuditableEntity
{
    private User() { }

    public User(string email, string passwordHash)
    {
        SetEmail(email);
        SetPasswordHash(passwordHash);
    }

    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        Email = email.Trim().ToLowerInvariant();
        Touch();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("PasswordHash is required.");

        PasswordHash = passwordHash;
        Touch();
    }
}
