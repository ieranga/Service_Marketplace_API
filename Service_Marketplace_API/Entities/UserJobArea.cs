namespace Service_Marketplace_API.Entities;

public class UserJobArea
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserJobId { get; set; }

    public string CityName { get; set; } = string.Empty;

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    // Navigation property
    public UserJob? UserJob { get; set; }
}
