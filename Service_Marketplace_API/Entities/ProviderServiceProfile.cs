namespace Service_Marketplace_API.Entities;

public class ProviderServiceProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string? BusinessName { get; set; }

    public string? BusinessRegistrationNumber { get; set; }

    public string? Bio { get; set; }

    public bool IsVerified { get; set; } = false;

    public string VerificationStatus { get; set; } = "Pending"; // Pending, Verified, Rejected

    public double RatingAverage { get; set; } = 5.0;

    public int ReviewCount { get; set; } = 0;

    public int CompletedJobsCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }

    public ICollection<ProviderService> Services { get; set; } = new List<ProviderService>();

    public ICollection<ProviderServiceArea> ServiceAreas { get; set; } = new List<ProviderServiceArea>();
}
