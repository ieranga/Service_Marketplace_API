namespace Service_Marketplace_API.Entities;

public class ProviderServiceTag
{
    public Guid ProviderServiceId { get; set; }
    public ProviderService? ProviderService { get; set; }

    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}
