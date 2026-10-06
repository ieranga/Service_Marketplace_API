using Service_Marketplace_API.DTOs.AI;
using Service_Marketplace_API.DTOs.Common;

namespace Service_Marketplace_API.Services.AI;

public interface IAIServiceDiscoveryService
{
    Task<ApiResponse<ServiceDiscoveryResponseDto>> DiscoverServicesAsync(ServiceDiscoveryRequestDto request);
}
