using Service_Marketplace_API.Entities;

namespace Service_Marketplace_API.Services.Common;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateJwtToken(User user);
}
