namespace Service_Marketplace_API.Entities;

public class ProviderService
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProviderServiceProfileId { get; set; }

    public int ServiceVariantId { get; set; }

    public decimal StartingPrice { get; set; }

    public string PriceUnit { get; set; } = "Per Job"; // Per Job, Per Hour, Per Visit

    public string? Description { get; set; }

    public bool SupportsHomeVisit { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ProviderServiceProfile? ProviderServiceProfile { get; set; }

    public ServiceVariant? ServiceVariant { get; set; }

    public ICollection<ProviderServiceTag> ProviderServiceTags { get; set; } = new List<ProviderServiceTag>();
}
