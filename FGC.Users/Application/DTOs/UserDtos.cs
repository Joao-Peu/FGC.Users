namespace FGC.Users.Application.DTOs;

public record RegisterRequest(string Email, string Password, string? FullName);
public record LoginRequest(string Email, string Password);
public record UserResponse(Guid Id, string Email, string? FullName, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public record LoginResponse(string Token, DateTime ExpiresAtUtc);
