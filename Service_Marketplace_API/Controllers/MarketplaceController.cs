using Microsoft.AspNetCore.Mvc;
using Service_Marketplace_API.DTOs.Common;
using Service_Marketplace_API.DTOs.Marketplace;
using Service_Marketplace_API.Services.Marketplace;

namespace Service_Marketplace_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _marketplaceService;

    public MarketplaceController(IMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    /// <summary>
    /// Retrieve the full Category -> Service -> Variant hierarchy.
    /// </summary>
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFullCatalog()
    {
        var result = await _marketplaceService.GetFullCatalogAsync();
        return Ok(result);
    }

    /// <summary>
    /// Retrieve all active categories.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _marketplaceService.GetCategoriesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Retrieve a category by ID with its services and variants.
    /// </summary>
    [HttpGet("categories/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var result = await _marketplaceService.GetCategoryByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Retrieve services under a specific category.
    /// </summary>
    [HttpGet("categories/{categoryId:int}/services")]
    [ProducesResponseType(typeof(ApiResponse<List<ServiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServicesByCategory(int categoryId)
    {
        var result = await _marketplaceService.GetServicesByCategoryAsync(categoryId);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve variants under a specific service.
    /// </summary>
    [HttpGet("services/{serviceId:int}/variants")]
    [ProducesResponseType(typeof(ApiResponse<List<ServiceVariantDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVariantsByService(int serviceId)
    {
        var result = await _marketplaceService.GetVariantsByServiceAsync(serviceId);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve all marketplace tags.
    /// </summary>
    [HttpGet("tags")]
    [ProducesResponseType(typeof(ApiResponse<List<TagDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTags()
    {
        var result = await _marketplaceService.GetAllTagsAsync();
        return Ok(result);
    }
}
