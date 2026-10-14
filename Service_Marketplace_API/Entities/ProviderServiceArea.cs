namespace Service_Marketplace_API.Entities;

public class ProviderServiceArea
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProviderServiceProfileId { get; set; }

    public string CityName { get; set; } = string.Empty;

    public double? RadiusKm { get; set; } = 15;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Navigation property
    public ProviderServiceProfile? ProviderServiceProfile { get; set; }
}
