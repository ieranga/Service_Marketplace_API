using System.ComponentModel.DataAnnotations;

namespace Service_Marketplace_API.DTOs.Auth;

public class CreateServiceProfileDto
{
    [StringLength(150)]
    public string? BusinessName { get; set; }

    [StringLength(50)]
    public string? BusinessRegistrationNumber { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }

    public List<string>? ServiceAreaCities { get; set; } = new();
}
