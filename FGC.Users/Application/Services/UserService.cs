using FGC.Users.Application.DTOs;
using FGC.Users.Application.Interfaces;
using FGC.Users.Domain.Entities;
using FGC.Users.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FGC.Users.Application.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly IEventPublisher _publisher;
    private readonly IConfiguration _config;

    public UserService(ApplicationDbContext db, IAuditService audit, IEventPublisher publisher, IConfiguration config)
    {
        _db = db;
        _audit = audit;
        _publisher = publisher;
        _config = config;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request, string correlationId, string? userId = null)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existing is not null)
            throw new InvalidOperationException("Email already registered");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _audit.AuditAsync("User", user.Id, "UserRegistered", null, user, correlationId, userId);

        // publish integration event
        var @event = new Domain.Events.UserRegistered(user.Id, user.Email, user.FullName);
        await _publisher.PublishAsync("UserRegistered", @event, correlationId);

        return new UserResponse(user.Id, user.Email, user.FullName, user.CreatedAtUtc, user.UpdatedAtUtc);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string correlationId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid credentials");

        var token = GenerateJwt(user);
        var expiresAt = DateTime.UtcNow.AddHours(1);
        return new LoginResponse(token, expiresAt);
    }

    public async Task<UserResponse> GetMeAsync(string userId)
    {
        var id = Guid.Parse(userId);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) throw new KeyNotFoundException("User not found");
        return new UserResponse(user.Id, user.Email, user.FullName, user.CreatedAtUtc, user.UpdatedAtUtc);
    }

    public async Task<UserResponse> UpdateMeAsync(string userId, RegisterRequest request, string correlationId)
    {
        var id = Guid.Parse(userId);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) throw new KeyNotFoundException("User not found");

        var before = new { user.FullName };

        user.FullName = request.FullName ?? user.FullName;
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.AuditAsync("User", user.Id, "UserProfileUpdated", before, user, correlationId, userId);
        var @event = new Domain.Events.UserProfileUpdated(user.Id, user.FullName);
        await _publisher.PublishAsync("UserProfileUpdated", @event, correlationId);

        return new UserResponse(user.Id, user.Email, user.FullName, user.CreatedAtUtc, user.UpdatedAtUtc);
    }

    private string GenerateJwt(User user)
    {
        var issuer = _config.GetValue<string>("Jwt:Issuer") ?? "fgc.local";
        var audience = _config.GetValue<string>("Jwt:Audience") ?? "fgc.clients";
        var secret = _config.GetValue<string>("Jwt:Secret") ?? "super-secret-key-please-change";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(issuer: issuer, audience: audience, subject: null, notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);
        return handler.WriteToken(token);
    }
}
