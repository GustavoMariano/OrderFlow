namespace OrderFlow.Application.DTOs.Auth;

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);
