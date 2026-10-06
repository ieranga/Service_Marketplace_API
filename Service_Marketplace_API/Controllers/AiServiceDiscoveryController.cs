using Microsoft.AspNetCore.Mvc;
using Service_Marketplace_API.DTOs.AI;
using Service_Marketplace_API.DTOs.Common;
using Service_Marketplace_API.Services.AI;

namespace Service_Marketplace_API.Controllers;

[ApiController]
[Route("api/ai")]
public class AiServiceDiscoveryController : ControllerBase
{
    private readonly IAIServiceDiscoveryService _aiService;

    public AiServiceDiscoveryController(IAIServiceDiscoveryService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>
    /// AI Service Discovery: Extracts structured intent and matches relevant providers from natural language.
    /// Example: "I need someone to wash my car at my home in Negombo this weekend"
    [HttpPost("discover")]
    [ProducesResponseType(typeof(ApiResponse<ServiceDiscoveryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ServiceDiscoveryResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DiscoverServices([FromBody] ServiceDiscoveryRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<ServiceDiscoveryResponseDto>.Fail("Validation failed.", errors));
        }

        var result = await _aiService.DiscoverServicesAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
