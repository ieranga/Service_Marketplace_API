namespace Service_Marketplace_API.DTOs.Auth;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? MaskedNIC { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string Role { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasServiceProfile { get; set; }
    public DateTime CreatedAt { get; set; }
}
