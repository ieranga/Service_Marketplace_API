using Service_Marketplace_API.Entities.Enums;

namespace Service_Marketplace_API.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? NIC { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public UserProfile? Profile { get; set; }
    public ProviderServiceProfile? ServiceProfile { get; set; }
    public ICollection<UserJob> Jobs { get; set; } = new List<UserJob>();
}

