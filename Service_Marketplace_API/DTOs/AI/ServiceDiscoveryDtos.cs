using System.ComponentModel.DataAnnotations;

namespace Service_Marketplace_API.DTOs.AI;

public class ServiceDiscoveryRequestDto
{
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Query { get; set; } = string.Empty;

    public double? UserLatitude { get; set; }
    public double? UserLongitude { get; set; }
    public string? PreferredLocation { get; set; }
}

public class ExtractedRequirementsDto
{
    public string RawQuery { get; set; } = string.Empty;
    public string DetectedIntent { get; set; } = string.Empty;

    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public int? ServiceId { get; set; }
    public string? ServiceName { get; set; }

    public int? VariantId { get; set; }
    public string? VariantName { get; set; }

    public List<string> ExtractedTags { get; set; } = new();
    public string? Location { get; set; }
    public decimal? MaxBudget { get; set; }
    public string? UrgencyOrDate { get; set; }
    public double ConfidenceScore { get; set; } = 1.0;
    public bool IsCatalogMatched { get; set; }
}

public class MatchedProviderDto
{
    public Guid ProviderServiceId { get; set; }
    public Guid ProviderServiceProfileId { get; set; }
    public Guid ProviderProfileId { get => ProviderServiceProfileId; set => ProviderServiceProfileId = value; }
    public string ProviderName { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public bool IsVerified { get; set; }
    public double RatingAverage { get; set; }
    public int ReviewCount { get; set; }

    public string CategoryName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;

    public decimal StartingPrice { get; set; }
    public string PriceUnit { get; set; } = "Per Job";
    public bool SupportsHomeVisit { get; set; }
    public List<string> ServiceAreas { get; set; } = new();
    public List<string> MatchedTags { get; set; } = new();

    public double MatchScore { get; set; }
    public string MatchExplanation { get; set; } = string.Empty;
}

public class ServiceDiscoveryResponseDto
{
    public ExtractedRequirementsDto ExtractedRequirements { get; set; } = new();
    public List<MatchedProviderDto> MatchedProviders { get; set; } = new();
    public int TotalMatches { get; set; }
    public string EngineUsed { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
