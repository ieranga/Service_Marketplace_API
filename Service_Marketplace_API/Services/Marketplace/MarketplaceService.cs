using Microsoft.EntityFrameworkCore;
using Service_Marketplace_API.Data;
using Service_Marketplace_API.DTOs.Common;
using Service_Marketplace_API.DTOs.Marketplace;
using Service_Marketplace_API.Entities;

namespace Service_Marketplace_API.Services.Marketplace;

public class MarketplaceService : IMarketplaceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MarketplaceService> _logger;

    public MarketplaceService(ApplicationDbContext context, ILogger<MarketplaceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CategoryDto>>> GetFullCatalogAsync()
    {
        try
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .Include(c => c.Services.Where(s => s.IsActive))
                    .ThenInclude(s => s.Variants.Where(v => v.IsActive))
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    Icon = c.Icon,
                    Services = c.Services.Select(s => new ServiceDto
                    {
                        Id = s.Id,
                        CategoryId = s.CategoryId,
                        CategoryName = c.Name,
                        Name = s.Name,
                        Slug = s.Slug,
                        Description = s.Description,
                        Variants = s.Variants.Select(v => new ServiceVariantDto
                        {
                            Id = v.Id,
                            ServiceId = v.ServiceId,
                            ServiceName = s.Name,
                            CategoryName = c.Name,
                            Name = v.Name,
                            Slug = v.Slug,
                            Description = v.Description,
                            SuggestedStartingPrice = v.SuggestedStartingPrice
                        }).ToList()
                    }).ToList()
                })
                .ToListAsync();

            return ApiResponse<List<CategoryDto>>.Ok(categories, "Full marketplace catalog retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving full catalog.");
            return ApiResponse<List<CategoryDto>>.Fail("Failed to retrieve marketplace catalog.");
        }
    }

    public async Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync()
    {
        try
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    Icon = c.Icon
                })
                .ToListAsync();

            return ApiResponse<List<CategoryDto>>.Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories.");
            return ApiResponse<List<CategoryDto>>.Fail("Failed to retrieve categories.");
        }
    }

    public async Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(int id)
    {
        try
        {
            var category = await _context.Categories
                .Where(c => c.Id == id && c.IsActive)
                .Include(c => c.Services.Where(s => s.IsActive))
                    .ThenInclude(s => s.Variants.Where(v => v.IsActive))
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    Icon = c.Icon,
                    Services = c.Services.Select(s => new ServiceDto
                    {
                        Id = s.Id,
                        CategoryId = s.CategoryId,
                        CategoryName = c.Name,
                        Name = s.Name,
                        Slug = s.Slug,
                        Description = s.Description,
                        Variants = s.Variants.Select(v => new ServiceVariantDto
                        {
                            Id = v.Id,
                            ServiceId = v.ServiceId,
                            ServiceName = s.Name,
                            CategoryName = c.Name,
                            Name = v.Name,
                            Slug = v.Slug,
                            Description = v.Description,
                            SuggestedStartingPrice = v.SuggestedStartingPrice
                        }).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return ApiResponse<CategoryDto>.Fail("Category not found.");
            }

            return ApiResponse<CategoryDto>.Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category {Id}.", id);
            return ApiResponse<CategoryDto>.Fail("Failed to retrieve category.");
        }
    }

    public async Task<ApiResponse<List<ServiceDto>>> GetServicesByCategoryAsync(int categoryId)
    {
        try
        {
            var services = await _context.Services
                .Where(s => s.CategoryId == categoryId && s.IsActive)
                .Include(s => s.Category)
                .Include(s => s.Variants.Where(v => v.IsActive))
                .OrderBy(s => s.Name)
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    CategoryId = s.CategoryId,
                    CategoryName = s.Category != null ? s.Category.Name : string.Empty,
                    Name = s.Name,
                    Slug = s.Slug,
                    Description = s.Description,
                    Variants = s.Variants.Select(v => new ServiceVariantDto
                    {
                        Id = v.Id,
                        ServiceId = v.ServiceId,
                        ServiceName = s.Name,
                        CategoryName = s.Category != null ? s.Category.Name : string.Empty,
                        Name = v.Name,
                        Slug = v.Slug,
                        Description = v.Description,
                        SuggestedStartingPrice = v.SuggestedStartingPrice
                    }).ToList()
                })
                .ToListAsync();

            return ApiResponse<List<ServiceDto>>.Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving services for category {CategoryId}.", categoryId);
            return ApiResponse<List<ServiceDto>>.Fail("Failed to retrieve services.");
        }
    }

    public async Task<ApiResponse<List<ServiceVariantDto>>> GetVariantsByServiceAsync(int serviceId)
    {
        try
        {
            var variants = await _context.ServiceVariants
                .Where(v => v.ServiceId == serviceId && v.IsActive)
                .Include(v => v.Service)
                    .ThenInclude(s => s!.Category)
                .Include(v => v.VariantTags)
                    .ThenInclude(vt => vt.Tag)
                .OrderBy(v => v.Name)
                .Select(v => new ServiceVariantDto
                {
                    Id = v.Id,
                    ServiceId = v.ServiceId,
                    ServiceName = v.Service != null ? v.Service.Name : string.Empty,
                    CategoryName = v.Service != null && v.Service.Category != null ? v.Service.Category.Name : string.Empty,
                    Name = v.Name,
                    Slug = v.Slug,
                    Description = v.Description,
                    SuggestedStartingPrice = v.SuggestedStartingPrice,
                    Tags = v.VariantTags.Where(vt => vt.Tag != null).Select(vt => vt.Tag!.Name).ToList()
                })
                .ToListAsync();

            return ApiResponse<List<ServiceVariantDto>>.Ok(variants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving variants for service {ServiceId}.", serviceId);
            return ApiResponse<List<ServiceVariantDto>>.Fail("Failed to retrieve service variants.");
        }
    }

    public async Task<ApiResponse<List<TagDto>>> GetAllTagsAsync()
    {
        try
        {
            var tags = await _context.Tags
                .Where(t => t.IsActive)
                .OrderBy(t => t.TagGroup).ThenBy(t => t.Name)
                .Select(t => new TagDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    TagGroup = t.TagGroup
                })
                .ToListAsync();

            return ApiResponse<List<TagDto>>.Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tags.");
            return ApiResponse<List<TagDto>>.Fail("Failed to retrieve tags.");
        }
    }

    public async Task<ApiResponse<List<ProviderServiceListingDto>>> SearchProviderServicesAsync(MarketplaceSearchFilterDto filter)
    {
        try
        {
            var query = _context.ProviderServices
                .Where(ps => ps.IsActive)
                .Include(ps => ps.ProviderServiceProfile)
                    .ThenInclude(p => p!.User)
                .Include(ps => ps.ProviderServiceProfile)
                    .ThenInclude(p => p!.ServiceAreas)
                .Include(ps => ps.ServiceVariant)
                    .ThenInclude(v => v!.Service)
                        .ThenInclude(s => s!.Category)
                .Include(ps => ps.ProviderServiceTags)
                    .ThenInclude(pst => pst.Tag)
                .AsQueryable();

            // 1. Variant filter
            if (filter.VariantId.HasValue)
            {
                query = query.Where(ps => ps.ServiceVariantId == filter.VariantId.Value);
            }
            // 2. Service filter
            else if (filter.ServiceId.HasValue)
            {
                query = query.Where(ps => ps.ServiceVariant != null && ps.ServiceVariant.ServiceId == filter.ServiceId.Value);
            }
            // 3. Category filter
            else if (filter.CategoryId.HasValue)
            {
                query = query.Where(ps => ps.ServiceVariant != null && ps.ServiceVariant.Service != null && ps.ServiceVariant.Service.CategoryId == filter.CategoryId.Value);
            }

            // 4. Max Price filter
            if (filter.MaxPrice.HasValue && filter.MaxPrice.Value > 0)
            {
                query = query.Where(ps => ps.StartingPrice <= filter.MaxPrice.Value);
            }

            // 5. Verification filter
            if (filter.VerifiedOnly.HasValue && filter.VerifiedOnly.Value)
            {
                query = query.Where(ps => ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.IsVerified);
            }

            // 6. Location filter
            if (!string.IsNullOrWhiteSpace(filter.Location))
            {
                var loc = filter.Location.Trim().ToLower();
                query = query.Where(ps => ps.ProviderServiceProfile != null && 
                    ps.ProviderServiceProfile.ServiceAreas.Any(sa => sa.CityName.ToLower().Contains(loc)));
            }

            // 7. General search term
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(ps =>
                    (ps.Description != null && ps.Description.ToLower().Contains(term)) ||
                    (ps.ServiceVariant != null && ps.ServiceVariant.Name.ToLower().Contains(term)) ||
                    (ps.ServiceVariant != null && ps.ServiceVariant.Service != null && ps.ServiceVariant.Service.Name.ToLower().Contains(term)) ||
                    (ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.User != null && ps.ProviderServiceProfile.User.FullName.ToLower().Contains(term))
                );
            }

            var results = await query
                .OrderByDescending(ps => ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.IsVerified)
                .ThenByDescending(ps => ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.RatingAverage : 0)
                .Select(ps => new ProviderServiceListingDto
                {
                    Id = ps.Id,
                    ProviderServiceProfileId = ps.ProviderServiceProfileId,
                    ProviderName = ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.User != null ? ps.ProviderServiceProfile.User.FullName : "Service Provider",
                    BusinessName = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.BusinessName : null,
                    ProviderBio = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.Bio : null,
                    IsVerified = ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.IsVerified,
                    RatingAverage = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.RatingAverage : 0.0,
                    ReviewCount = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.ReviewCount : 0,
                    CompletedJobsCount = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.CompletedJobsCount : 0,
                    CategoryId = ps.ServiceVariant != null && ps.ServiceVariant.Service != null ? ps.ServiceVariant.Service.CategoryId : 0,
                    CategoryName = ps.ServiceVariant != null && ps.ServiceVariant.Service != null && ps.ServiceVariant.Service.Category != null ? ps.ServiceVariant.Service.Category.Name : string.Empty,
                    ServiceId = ps.ServiceVariant != null ? ps.ServiceVariant.ServiceId : 0,
                    ServiceName = ps.ServiceVariant != null && ps.ServiceVariant.Service != null ? ps.ServiceVariant.Service.Name : string.Empty,
                    VariantId = ps.ServiceVariantId,
                    VariantName = ps.ServiceVariant != null ? ps.ServiceVariant.Name : string.Empty,
                    StartingPrice = ps.StartingPrice,
                    PriceUnit = ps.PriceUnit,
                    Description = ps.Description,
                    SupportsHomeVisit = ps.SupportsHomeVisit,
                    ServiceAreas = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.ServiceAreas.Select(sa => sa.CityName).ToList() : new List<string>(),
                    Tags = ps.ProviderServiceTags.Where(pst => pst.Tag != null).Select(pst => pst.Tag!.Name).ToList()
                })
                .ToListAsync();

            return ApiResponse<List<ProviderServiceListingDto>>.Ok(results, $"Found {results.Count} provider service listings.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching provider services.");
            return ApiResponse<List<ProviderServiceListingDto>>.Fail("Failed to search provider listings.");
        }
    }

    public async Task<ApiResponse<ProviderServiceListingDto>> GetProviderServiceByIdAsync(Guid id)
    {
        try
        {
            var ps = await _context.ProviderServices
                .Where(p => p.Id == id && p.IsActive)
                .Include(p => p.ProviderServiceProfile)
                    .ThenInclude(pp => pp!.User)
                .Include(p => p.ProviderServiceProfile)
                    .ThenInclude(pp => pp!.ServiceAreas)
                .Include(p => p.ServiceVariant)
                    .ThenInclude(v => v!.Service)
                        .ThenInclude(s => s!.Category)
                .Include(p => p.ProviderServiceTags)
                    .ThenInclude(pst => pst.Tag)
                .Select(p => new ProviderServiceListingDto
                {
                    Id = p.Id,
                    ProviderServiceProfileId = p.ProviderServiceProfileId,
                    ProviderName = p.ProviderServiceProfile != null && p.ProviderServiceProfile.User != null ? p.ProviderServiceProfile.User.FullName : "Service Provider",
                    BusinessName = p.ProviderServiceProfile != null ? p.ProviderServiceProfile.BusinessName : null,
                    ProviderBio = p.ProviderServiceProfile != null ? p.ProviderServiceProfile.Bio : null,
                    IsVerified = p.ProviderServiceProfile != null && p.ProviderServiceProfile.IsVerified,
                    RatingAverage = p.ProviderServiceProfile != null ? p.ProviderServiceProfile.RatingAverage : 0.0,
                    ReviewCount = p.ProviderServiceProfile != null ? p.ProviderServiceProfile.ReviewCount : 0,
                    CompletedJobsCount = p.ProviderServiceProfile != null ? p.ProviderServiceProfile.CompletedJobsCount : 0,
                    CategoryId = p.ServiceVariant != null && p.ServiceVariant.Service != null ? p.ServiceVariant.Service.CategoryId : 0,
                    CategoryName = p.ServiceVariant != null && p.ServiceVariant.Service != null && p.ServiceVariant.Service.Category != null ? p.ServiceVariant.Service.Category.Name : string.Empty,
                    ServiceId = p.ServiceVariant != null ? p.ServiceVariant.ServiceId : 0,
                    ServiceName = p.ServiceVariant != null && p.ServiceVariant.Service != null ? p.ServiceVariant.Service.Name : string.Empty,
                    VariantId = p.ServiceVariantId,
                    VariantName = p.ServiceVariant != null ? p.ServiceVariant.Name : string.Empty,
                    StartingPrice = p.StartingPrice,
                    PriceUnit = p.PriceUnit,
                    Description = p.Description,
                    SupportsHomeVisit = p.SupportsHomeVisit,
                    ServiceAreas = p.ProviderServiceProfile != null ? p.ProviderServiceProfile.ServiceAreas.Select(sa => sa.CityName).ToList() : new List<string>(),
                    Tags = p.ProviderServiceTags.Where(pst => pst.Tag != null).Select(pst => pst.Tag!.Name).ToList()
                })
                .FirstOrDefaultAsync();

            if (ps == null)
            {
                return ApiResponse<ProviderServiceListingDto>.Fail("Provider service listing not found.");
            }

            return ApiResponse<ProviderServiceListingDto>.Ok(ps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider service listing {Id}.", id);
            return ApiResponse<ProviderServiceListingDto>.Fail("Failed to retrieve provider service listing.");
        }
    }

    public async Task<ApiResponse<ProviderServiceListingDto>> CreateProviderServiceAsync(Guid userId, CreateProviderServiceDto dto)
    {
        try
        {
            // 1. Verify user exists
            var user = await _context.Users
                .Include(u => u.ServiceProfile)
                    .ThenInclude(p => p!.ServiceAreas)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ApiResponse<ProviderServiceListingDto>.Fail("User not found.");
            }

            // 2. Single account principle: Ensure ServiceProfile exists for this user
            if (user.ServiceProfile == null)
            {
                user.ServiceProfile = new ProviderServiceProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    BusinessName = user.FullName,
                    Bio = "Independent Service Provider",
                    IsVerified = false,
                    VerificationStatus = "Pending",
                    RatingAverage = 5.0,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.ProviderServiceProfiles.AddAsync(user.ServiceProfile);
                await _context.SaveChangesAsync();
            }

            // 3. Verify Variant exists
            var variant = await _context.ServiceVariants
                .Include(v => v.Service)
                    .ThenInclude(s => s!.Category)
                .FirstOrDefaultAsync(v => v.Id == dto.ServiceVariantId && v.IsActive);

            if (variant == null)
            {
                return ApiResponse<ProviderServiceListingDto>.Fail("Selected service variant does not exist or is inactive.");
            }

            // 4. Create ProviderService
            var providerService = new ProviderService
            {
                Id = Guid.NewGuid(),
                ProviderServiceProfileId = user.ServiceProfile.Id,
                ServiceVariantId = dto.ServiceVariantId,
                StartingPrice = dto.StartingPrice,
                PriceUnit = dto.PriceUnit,
                Description = dto.Description,
                SupportsHomeVisit = dto.SupportsHomeVisit,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // 5. Add Tags
            if (dto.TagIds.Any())
            {
                var validTagIds = await _context.Tags
                    .Where(t => dto.TagIds.Contains(t.Id))
                    .Select(t => t.Id)
                    .ToListAsync();

                foreach (var tagId in validTagIds)
                {
                    providerService.ProviderServiceTags.Add(new ProviderServiceTag
                    {
                        TagId = tagId
                    });
                }
            }

            // 6. Add Service Areas if provided
            if (dto.ServiceAreaCities.Any())
            {
                foreach (var city in dto.ServiceAreaCities.Distinct())
                {
                    if (!user.ServiceProfile.ServiceAreas.Any(sa => sa.CityName.Equals(city, StringComparison.OrdinalIgnoreCase)))
                    {
                        user.ServiceProfile.ServiceAreas.Add(new ProviderServiceArea
                        {
                            CityName = city.Trim(),
                            RadiusKm = 15
                        });
                    }
                }
            }

            await _context.ProviderServices.AddAsync(providerService);
            await _context.SaveChangesAsync();

            return await GetProviderServiceByIdAsync(providerService.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provider service listing.");
            return ApiResponse<ProviderServiceListingDto>.Fail("Failed to create provider service listing.");
        }
    }
}
