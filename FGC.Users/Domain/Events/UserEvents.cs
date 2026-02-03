namespace FGC.Users.Domain.Events;

public record UserRegistered(Guid UserId, string Email, string? FullName);
public record UserProfileUpdated(Guid UserId, string? FullName);
