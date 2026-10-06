using Service_Marketplace_API.DTOs.Common;
using Service_Marketplace_API.DTOs.Marketplace;

namespace Service_Marketplace_API.Services.Marketplace;

public interface IListingService
{
    // Provider Service Listings
    Task<ApiResponse<ProviderServiceListingDto>> CreateServiceAsync(Guid userId, CreateProviderServiceDto dto);
    Task<ApiResponse<bool>> DeleteServiceAsync(Guid userId, Guid serviceId);
    Task<ApiResponse<List<ProviderServiceListingDto>>> SearchServicesAsync(MarketplaceSearchFilterDto filter);
    Task<ApiResponse<ProviderServiceListingDto>> GetServiceByIdAsync(Guid id);
    Task<ApiResponse<List<ProviderServiceListingDto>>> GetUserServicesAsync(Guid userId);

    // Receiver Job Listings
    Task<ApiResponse<ReceiverJobDto>> CreateJobAsync(Guid userId, CreateReceiverJobDto dto);
    Task<ApiResponse<bool>> DeleteJobAsync(Guid userId, Guid jobId);
    Task<ApiResponse<List<ReceiverJobDto>>> SearchJobsAsync(JobSearchFilterDto filter);
    Task<ApiResponse<ReceiverJobDto>> GetJobByIdAsync(Guid id);
    Task<ApiResponse<List<ReceiverJobDto>>> GetUserJobsAsync(Guid userId);
}
