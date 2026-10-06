namespace Service_Marketplace_API.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasServiceProfile { get; set; }
    public DateTime ExpiresAt { get; set; }
}
