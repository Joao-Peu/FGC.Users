using FGC.Users.Application.DTOs;
using FGC.Users.Domain.Entities;

namespace FGC.Users.Tests.Fixtures;

/// <summary>
/// Dados de teste reutilizáveis
/// </summary>
public static class TestData
{
    public static class Users
    {
        public static User ValidUser => new()
        {
            Id = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            FullName = "Test User",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        public static User AnotherUser => new()
        {
            Id = Guid.Parse("223e4567-e89b-12d3-a456-426614174001"),
            Email = "another@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password456"),
            FullName = "Another User",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public static class Requests
    {
        public static RegisterRequest ValidRegisterRequest => new(
            Email: "newuser@example.com",
            Password: "ValidPassword123!",
            FullName: "New User"
        );

        public static RegisterRequest InvalidEmailRequest => new(
            Email: "invalid-email",
            Password: "ValidPassword123!",
            FullName: "New User"
        );

        public static RegisterRequest ShortPasswordRequest => new(
            Email: "newuser@example.com",
            Password: "123",
            FullName: "New User"
        );

        public static LoginRequest ValidLoginRequest => new(
            Email: "test@example.com",
            Password: "password123"
        );

        public static LoginRequest InvalidLoginRequest => new(
            Email: "test@example.com",
            Password: "wrongpassword"
        );
    }

    public static class CorrelationIds
    {
        public static string Valid => Guid.NewGuid().ToString();
    }
}
