using Service_Marketplace_API.DTOs.Auth;
using Service_Marketplace_API.DTOs.Common;

namespace Service_Marketplace_API.Services.Auth;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<List<UserResponseDto>>> GetAllUsersAsync();
    Task<ApiResponse<UserResponseDto>> CreateAdminAsync(CreateAdminRequestDto request);
    Task<ApiResponse<ServiceProfileResponseDto>> CreateServiceProfileAsync(Guid userId, CreateServiceProfileDto dto);
    Task<ApiResponse<ServiceProfileResponseDto>> UpdateServiceProfileAsync(Guid userId, UpdateServiceProfileDto dto);
}
