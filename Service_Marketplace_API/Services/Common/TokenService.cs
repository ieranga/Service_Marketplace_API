using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Service_Marketplace_API.Entities;

namespace Service_Marketplace_API.Services.Common;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) GenerateJwtToken(User user)
    {
        var secretKey = _configuration["Jwt:Key"] ?? "ServiceMarketplaceSuperSecretKey2026!#SecureDefaultSecretKeyMustBeLongEnough";
        var issuer = _configuration["Jwt:Issuer"] ?? "ServiceMarketplaceAPI";
        var audience = _configuration["Jwt:Audience"] ?? "ServiceMarketplaceClient";
        var expiryMinutes = int.TryParse(_configuration["Jwt:DurationInMinutes"], out var minutes) ? minutes : 1440; // 24 hours

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString()),
            new("roleId", ((int)user.Role).ToString()),
            new("status", user.Status.ToString()),
            new("phoneNumber", user.PhoneNumber)
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.WriteToken(tokenDescriptor);

        return (token, expiresAt);
    }
}
