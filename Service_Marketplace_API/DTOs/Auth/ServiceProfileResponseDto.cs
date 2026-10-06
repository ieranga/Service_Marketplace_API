namespace Service_Marketplace_API.DTOs.Auth;

public class ServiceProfileResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? Bio { get; set; }
    public bool IsVerified { get; set; }
    public string VerificationStatus { get; set; } = "Pending";
    public double RatingAverage { get; set; }
    public int ReviewCount { get; set; }
    public int CompletedJobsCount { get; set; }
    public List<string> ServiceAreas { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}
