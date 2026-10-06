namespace Service_Marketplace_API.Entities;

public class ServiceVariantTag
{
    public int ServiceVariantId { get; set; }
    public ServiceVariant? ServiceVariant { get; set; }

    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}
