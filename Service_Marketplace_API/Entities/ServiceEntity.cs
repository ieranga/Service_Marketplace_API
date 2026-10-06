namespace Service_Marketplace_API.Entities;

public class ServiceEntity
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Category? Category { get; set; }

    public ICollection<ServiceVariant> Variants { get; set; } = new List<ServiceVariant>();
}
