namespace Service_Marketplace_API.Entities;

public class UserJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public int? ServiceVariantId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal? Budget { get; set; }

    public string? PaymentMethod { get; set; } = "Any"; // Cash, Online, Any

    public string Status { get; set; } = "Open"; // Open, Assigned, InProgress, Completed, Cancelled

    public string? UrgencyOrPreferredDate { get; set; }

    public DateTime? ExpectedDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }

    public ServiceVariant? ServiceVariant { get; set; }

    public ICollection<UserJobArea> JobAreas { get; set; } = new List<UserJobArea>();

    public ICollection<UserJobTag> JobTags { get; set; } = new List<UserJobTag>();
}
