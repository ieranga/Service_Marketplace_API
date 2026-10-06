namespace Service_Marketplace_API.Entities;

public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? TagGroup { get; set; } // e.g. "LocationType", "Timing", "Urgency", "Skill"

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<ServiceVariantTag> VariantTags { get; set; } = new List<ServiceVariantTag>();

    public ICollection<ProviderServiceTag> ProviderServiceTags { get; set; } = new List<ProviderServiceTag>();

    public ICollection<UserJobTag> UserJobTags { get; set; } = new List<UserJobTag>();
}
