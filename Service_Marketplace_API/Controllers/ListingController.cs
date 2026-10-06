using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service_Marketplace_API.DTOs.Common;
using Service_Marketplace_API.DTOs.Marketplace;
using Service_Marketplace_API.Services.Marketplace;

namespace Service_Marketplace_API.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingController : ControllerBase
{
    private readonly IListingService _listingService;

    public ListingController(IListingService listingService)
    {
        _listingService = listingService;
    }

    #region Provider Services

    /// <summary>
    /// Create a new provider service listing for the authenticated user.
    /// </summary>
    [Authorize]
    [HttpPost("createService")]
    [ProducesResponseType(typeof(ApiResponse<ProviderServiceListingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderServiceListingDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateService([FromBody] CreateProviderServiceDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ProviderServiceListingDto>.Fail("Validation failed.", errors));
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<ProviderServiceListingDto>.Fail("Invalid user identity in token."));
        }

        var result = await _listingService.CreateServiceAsync(userId.Value, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a provider service listing for the authenticated user.
    /// </summary>
    [Authorize]
    [HttpDelete("deleteService/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<bool>.Fail("Invalid user identity in token."));
        }

        var result = await _listingService.DeleteServiceAsync(userId.Value, id);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Search provider service listings using structured filters.
    /// </summary>
    [HttpGet("services")]
    [ProducesResponseType(typeof(ApiResponse<List<ProviderServiceListingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchServices([FromQuery] MarketplaceSearchFilterDto filter)
    {
        var result = await _listingService.SearchServicesAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve a specific provider service listing by ID.
    /// </summary>
    [HttpGet("services/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProviderServiceListingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderServiceListingDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceById(Guid id)
    {
        var result = await _listingService.GetServiceByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Retrieve all service listings created by the authenticated provider.
    /// </summary>
    [Authorize]
    [HttpGet("myServices")]
    [ProducesResponseType(typeof(ApiResponse<List<ProviderServiceListingDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyServices()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<List<ProviderServiceListingDto>>.Fail("Invalid user identity in token."));
        }

        var result = await _listingService.GetUserServicesAsync(userId.Value);
        return Ok(result);
    }

    #endregion

    #region Receiver Jobs

    /// <summary>
    /// Create a new receiver job listing for the authenticated user.
    /// </summary>
    [Authorize]
    [HttpPost("createJob")]
    [ProducesResponseType(typeof(ApiResponse<ReceiverJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReceiverJobDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateJob([FromBody] CreateReceiverJobDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ReceiverJobDto>.Fail("Validation failed.", errors));
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<ReceiverJobDto>.Fail("Invalid user identity in token."));
        }

        var result = await _listingService.CreateJobAsync(userId.Value, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a receiver job for the authenticated user.
    /// </summary>
    [Authorize]
    [HttpDelete("deleteJob/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<bool>.Fail("Invalid user identity in token."));
        }

        var result = await _listingService.DeleteJobAsync(userId.Value, id);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Search receiver job listings using filters.
    /// </summary>
    [HttpGet("searchJobs")]
    [ProducesResponseType(typeof(ApiResponse<List<ReceiverJobDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchJobs([FromQuery] JobSearchFilterDto filter)
    {
        var result = await _listingService.SearchJobsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve a specific receiver job by ID.
    /// </summary>
    [HttpGet("jobs/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ReceiverJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReceiverJobDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobById(Guid id)
    {
        var result = await _listingService.GetJobByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Retrieve all receiver jobs created by the authenticated user.
    /// </summary>
    [Authorize]
    [HttpGet("myJobs")]
    [ProducesResponseType(typeof(ApiResponse<List<ReceiverJobDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyJobs()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<List<ReceiverJobDto>>.Fail("Invalid user identity in token."));
        }

        var result = await _listingService.GetUserJobsAsync(userId.Value);
        return Ok(result);
    }

    #endregion

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }
}
