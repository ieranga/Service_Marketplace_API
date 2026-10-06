namespace Service_Marketplace_API.Entities;

public class ServiceVariant
{
    public int Id { get; set; }

    public int ServiceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal? SuggestedStartingPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ServiceEntity? Service { get; set; }

    public ICollection<ServiceVariantTag> VariantTags { get; set; } = new List<ServiceVariantTag>();

    public ICollection<ProviderService> ProviderServices { get; set; } = new List<ProviderService>();

    public ICollection<UserJob> UserJobs { get; set; } = new List<UserJob>();
}
