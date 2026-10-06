using System.ComponentModel.DataAnnotations;

namespace Service_Marketplace_API.DTOs.Marketplace;

public class CreateReceiverJobDto
{
    public int? ServiceVariantId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(0, 100000000)]
    public decimal? Budget { get; set; }

    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Any"; // Cash, Online, Any

    [StringLength(100)]
    public string? UrgencyOrPreferredDate { get; set; } // e.g. "Tomorrow", "This weekend", "Urgent"

    public DateTime? ExpectedDate { get; set; }

    public List<int> TagIds { get; set; } = new();

    public List<CreateJobAreaDto> JobAreas { get; set; } = new();
}

public class CreateJobAreaDto
{
    [Required]
    [StringLength(100)]
    public string CityName { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}

public class ReceiverJobDto
{
    public Guid Id { get; set; }
    public Guid ReceiverProfileId { get; set; }
    public Guid UserId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string? ReceiverPhone { get; set; }

    public int? ServiceVariantId { get; set; }
    public string? ServiceVariantName { get; set; }
    public string? ServiceName { get; set; }
    public string? CategoryName { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Budget { get; set; }
    public string PaymentMethod { get; set; } = "Any";
    public string Status { get; set; } = "Open";
    public string? UrgencyOrPreferredDate { get; set; }
    public DateTime? ExpectedDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<ReceiverJobAreaDto> JobAreas { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class ReceiverJobAreaDto
{
    public Guid Id { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class JobSearchFilterDto
{
    public int? CategoryId { get; set; }
    public int? ServiceId { get; set; }
    public int? VariantId { get; set; }
    public string? SearchTerm { get; set; }
    public string? Location { get; set; }
    public decimal? MaxBudget { get; set; }
    public string? Status { get; set; }
    public List<string>? Tags { get; set; }
}
