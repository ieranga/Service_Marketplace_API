using System.ComponentModel.DataAnnotations;

namespace Service_Marketplace_API.DTOs.Marketplace;

public class ProviderServiceListingDto
{
    public Guid Id { get; set; }
    public Guid ProviderServiceProfileId { get; set; }
    public Guid ProviderProfileId { get => ProviderServiceProfileId; set => ProviderServiceProfileId = value; }
    public string ProviderName { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public string? ProviderBio { get; set; }
    public bool IsVerified { get; set; }
    public double RatingAverage { get; set; }
    public int ReviewCount { get; set; }
    public int CompletedJobsCount { get; set; }

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int VariantId { get; set; }
    public string VariantName { get; set; } = string.Empty;

    public decimal StartingPrice { get; set; }
    public string PriceUnit { get; set; } = "Per Job";
    public string? Description { get; set; }
    public bool SupportsHomeVisit { get; set; }

    public List<string> ServiceAreas { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class CreateProviderServiceDto
{
    [Required]
    public int ServiceVariantId { get; set; }

    [Required]
    [Range(0, 10000000)]
    public decimal StartingPrice { get; set; }

    [Required]
    [StringLength(30)]
    public string PriceUnit { get; set; } = "Per Job";

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool SupportsHomeVisit { get; set; } = true;

    public List<int> TagIds { get; set; } = new();

    public List<string> ServiceAreaCities { get; set; } = new();
}

public class MarketplaceSearchFilterDto
{
    public int? CategoryId { get; set; }
    public int? ServiceId { get; set; }
    public int? VariantId { get; set; }
    public string? SearchTerm { get; set; }
    public string? Location { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? VerifiedOnly { get; set; }
    public List<string>? Tags { get; set; }
}
