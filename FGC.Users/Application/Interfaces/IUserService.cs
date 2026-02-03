using FGC.Users.Application.DTOs;

namespace FGC.Users.Application.Interfaces;

public interface IUserService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request, string correlationId, string? userId = null);
    Task<LoginResponse> LoginAsync(LoginRequest request, string correlationId);
    Task<UserResponse> GetMeAsync(string userId);
    Task<UserResponse> UpdateMeAsync(string userId, RegisterRequest request, string correlationId);
}
