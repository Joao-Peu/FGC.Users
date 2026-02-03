using System.ComponentModel.DataAnnotations;

namespace FGC.Users.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    [Required]
    public string Email { get; set; } = null!;
    [Required]
    public string PasswordHash { get; set; } = null!;
    public string? FullName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
