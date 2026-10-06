using Service_Marketplace_API.DTOs.Common;
using Service_Marketplace_API.DTOs.Marketplace;

namespace Service_Marketplace_API.Services.Marketplace;

public interface IMarketplaceService
{
    Task<ApiResponse<List<CategoryDto>>> GetFullCatalogAsync();
    Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync();
    Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(int id);
    Task<ApiResponse<List<ServiceDto>>> GetServicesByCategoryAsync(int categoryId);
    Task<ApiResponse<List<ServiceVariantDto>>> GetVariantsByServiceAsync(int serviceId);
    Task<ApiResponse<List<TagDto>>> GetAllTagsAsync();

    Task<ApiResponse<List<ProviderServiceListingDto>>> SearchProviderServicesAsync(MarketplaceSearchFilterDto filter);
    Task<ApiResponse<ProviderServiceListingDto>> GetProviderServiceByIdAsync(Guid id);
    Task<ApiResponse<ProviderServiceListingDto>> CreateProviderServiceAsync(Guid userId, CreateProviderServiceDto dto);
}
