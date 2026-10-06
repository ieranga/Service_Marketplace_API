using System.ComponentModel.DataAnnotations;

namespace Service_Marketplace_API.DTOs.Auth;

public class CreateAdminRequestDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(30, MinimumLength = 7)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [StringLength(30)]
    public string? NIC { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }
}
