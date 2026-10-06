using Service_Marketplace_API.DTOs.AI;

namespace Service_Marketplace_API.Services.AI;

public interface IMarketplaceMatchingService
{
    Task<List<MatchedProviderDto>> MatchProvidersAsync(ExtractedRequirementsDto requirements);
}
