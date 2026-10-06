using Microsoft.EntityFrameworkCore;
using Service_Marketplace_API.Data;
using Service_Marketplace_API.DTOs.Common;
using Service_Marketplace_API.DTOs.Marketplace;
using Service_Marketplace_API.Entities;
using Service_Marketplace_API.Entities.Enums;

namespace Service_Marketplace_API.Services.Marketplace;

public class ListingService : IListingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ListingService> _logger;

    public ListingService(ApplicationDbContext context, ILogger<ListingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Provider Service Listings

    public async Task<ApiResponse<ProviderServiceListingDto>> CreateServiceAsync(Guid userId, CreateProviderServiceDto dto)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.ServiceProfile)
                    .ThenInclude(p => p!.ServiceAreas)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ApiResponse<ProviderServiceListingDto>.Fail("User not found.");
            }

            if (user.Role == UserRole.Admin)
            {
                return ApiResponse<ProviderServiceListingDto>.Fail("Administrators cannot create service listings.");
            }

            // Single user account: ensure ServiceProfile exists
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

            var variant = await _context.ServiceVariants
                .Include(v => v.Service)
                    .ThenInclude(s => s!.Category)
                .FirstOrDefaultAsync(v => v.Id == dto.ServiceVariantId && v.IsActive);

            if (variant == null)
            {
                return ApiResponse<ProviderServiceListingDto>.Fail("Selected service variant does not exist or is inactive.");
            }

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

            return await GetServiceByIdAsync(providerService.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provider service listing.");
            return ApiResponse<ProviderServiceListingDto>.Fail("Failed to create provider service listing.");
        }
    }

    public async Task<ApiResponse<bool>> DeleteServiceAsync(Guid userId, Guid serviceId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("User not found.");
            }

            var service = await _context.ProviderServices
                .Include(ps => ps.ProviderServiceProfile)
                .FirstOrDefaultAsync(ps => ps.Id == serviceId);

            if (service == null)
            {
                return ApiResponse<bool>.Fail("Service listing not found.");
            }

            // Verify ownership or admin privileges
            var isOwner = service.ProviderServiceProfile != null && service.ProviderServiceProfile.UserId == userId;
            var isAdmin = user.Role == UserRole.SuperAdmin || user.Role == UserRole.Admin;

            if (!isOwner && !isAdmin)
            {
                return ApiResponse<bool>.Fail("You are not authorized to delete this service listing.");
            }

            service.IsActive = false;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Service listing deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting service listing {ServiceId}.", serviceId);
            return ApiResponse<bool>.Fail("Failed to delete service listing.");
        }
    }

    public async Task<ApiResponse<List<ProviderServiceListingDto>>> SearchServicesAsync(MarketplaceSearchFilterDto filter)
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

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(ps => ps.ServiceVariant != null &&
                                          ps.ServiceVariant.Service != null &&
                                          ps.ServiceVariant.Service.CategoryId == filter.CategoryId.Value);
            }

            if (filter.ServiceId.HasValue)
            {
                query = query.Where(ps => ps.ServiceVariant != null &&
                                          ps.ServiceVariant.ServiceId == filter.ServiceId.Value);
            }

            if (filter.VariantId.HasValue)
            {
                query = query.Where(ps => ps.ServiceVariantId == filter.VariantId.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(ps => ps.StartingPrice <= filter.MaxPrice.Value);
            }

            if (filter.VerifiedOnly.HasValue && filter.VerifiedOnly.Value)
            {
                query = query.Where(ps => ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.IsVerified);
            }

            if (!string.IsNullOrWhiteSpace(filter.Location))
            {
                var loc = filter.Location.Trim().ToLower();
                query = query.Where(ps => ps.ProviderServiceProfile != null &&
                    ps.ProviderServiceProfile.ServiceAreas.Any(sa => sa.CityName.ToLower().Contains(loc)));
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(ps =>
                    (ps.Description != null && ps.Description.ToLower().Contains(term)) ||
                    (ps.ServiceVariant != null && ps.ServiceVariant.Name.ToLower().Contains(term)) ||
                    (ps.ServiceVariant != null && ps.ServiceVariant.Service != null && ps.ServiceVariant.Service.Name.ToLower().Contains(term)) ||
                    (ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.BusinessName != null && ps.ProviderServiceProfile.BusinessName.ToLower().Contains(term)) ||
                    ps.ProviderServiceTags.Any(pst => pst.Tag != null && pst.Tag.Name.ToLower().Contains(term)));
            }

            if (filter.Tags != null && filter.Tags.Any())
            {
                var lowerTags = filter.Tags.Select(t => t.ToLower()).ToList();
                query = query.Where(ps => ps.ProviderServiceTags.Any(pst => pst.Tag != null && lowerTags.Contains(pst.Tag.Name.ToLower())));
            }

            var listings = await query
                .OrderByDescending(ps => ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.IsVerified)
                .ThenByDescending(ps => ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.RatingAverage : 0)
                .Select(ps => new ProviderServiceListingDto
                {
                    Id = ps.Id,
                    ProviderServiceProfileId = ps.ProviderServiceProfileId,
                    ProviderName = ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.User != null ? ps.ProviderServiceProfile.User.FullName : string.Empty,
                    BusinessName = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.BusinessName : null,
                    ProviderBio = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.Bio : null,
                    IsVerified = ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.IsVerified,
                    RatingAverage = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.RatingAverage : 5.0,
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

            return ApiResponse<List<ProviderServiceListingDto>>.Ok(listings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching provider service listings.");
            return ApiResponse<List<ProviderServiceListingDto>>.Fail("Failed to search provider service listings.");
        }
    }

    public async Task<ApiResponse<ProviderServiceListingDto>> GetServiceByIdAsync(Guid id)
    {
        try
        {
            var ps = await _context.ProviderServices
                .Where(p => p.Id == id)
                .Include(p => p.ProviderServiceProfile)
                    .ThenInclude(pr => pr!.User)
                .Include(p => p.ProviderServiceProfile)
                    .ThenInclude(pr => pr!.ServiceAreas)
                .Include(p => p.ServiceVariant)
                    .ThenInclude(v => v!.Service)
                        .ThenInclude(s => s!.Category)
                .Include(p => p.ProviderServiceTags)
                    .ThenInclude(pst => pst.Tag)
                .Select(p => new ProviderServiceListingDto
                {
                    Id = p.Id,
                    ProviderServiceProfileId = p.ProviderServiceProfileId,
                    ProviderName = p.ProviderServiceProfile != null && p.ProviderServiceProfile.User != null ? p.ProviderServiceProfile.User.FullName : string.Empty,
                    BusinessName = p.ProviderServiceProfile != null ? p.ProviderServiceProfile.BusinessName : null,
                    ProviderBio = p.ProviderServiceProfile != null ? p.ProviderServiceProfile.Bio : null,
                    IsVerified = p.ProviderServiceProfile != null && p.ProviderServiceProfile.IsVerified,
                    RatingAverage = p.ProviderServiceProfile != null ? p.ProviderServiceProfile.RatingAverage : 5.0,
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

    public async Task<ApiResponse<List<ProviderServiceListingDto>>> GetUserServicesAsync(Guid userId)
    {
        try
        {
            var listings = await _context.ProviderServices
                .Where(ps => ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.UserId == userId && ps.IsActive)
                .Include(ps => ps.ProviderServiceProfile)
                    .ThenInclude(p => p!.User)
                .Include(ps => ps.ProviderServiceProfile)
                    .ThenInclude(p => p!.ServiceAreas)
                .Include(ps => ps.ServiceVariant)
                    .ThenInclude(v => v!.Service)
                        .ThenInclude(s => s!.Category)
                .Include(ps => ps.ProviderServiceTags)
                    .ThenInclude(pst => pst.Tag)
                .OrderByDescending(ps => ps.CreatedAt)
                .Select(ps => new ProviderServiceListingDto
                {
                    Id = ps.Id,
                    ProviderServiceProfileId = ps.ProviderServiceProfileId,
                    ProviderName = ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.User != null ? ps.ProviderServiceProfile.User.FullName : string.Empty,
                    BusinessName = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.BusinessName : null,
                    ProviderBio = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.Bio : null,
                    IsVerified = ps.ProviderServiceProfile != null && ps.ProviderServiceProfile.IsVerified,
                    RatingAverage = ps.ProviderServiceProfile != null ? ps.ProviderServiceProfile.RatingAverage : 5.0,
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

            return ApiResponse<List<ProviderServiceListingDto>>.Ok(listings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user service listings for user {UserId}.", userId);
            return ApiResponse<List<ProviderServiceListingDto>>.Fail("Failed to retrieve user service listings.");
        }
    }

    #endregion

    #region User Job Listings (Main user account creates jobs directly)

    public async Task<ApiResponse<ReceiverJobDto>> CreateJobAsync(Guid userId, CreateReceiverJobDto dto)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ApiResponse<ReceiverJobDto>.Fail("User not found.");
            }

            if (user.Role == UserRole.Admin)
            {
                return ApiResponse<ReceiverJobDto>.Fail("Administrators cannot create jobs.");
            }

            // Validate service variant if provided
            if (dto.ServiceVariantId.HasValue)
            {
                var variantExists = await _context.ServiceVariants
                    .AnyAsync(v => v.Id == dto.ServiceVariantId.Value && v.IsActive);
                if (!variantExists)
                {
                    return ApiResponse<ReceiverJobDto>.Fail("Selected service variant does not exist or is inactive.");
                }
            }

            var job = new UserJob
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ServiceVariantId = dto.ServiceVariantId,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                Budget = dto.Budget,
                PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Any" : dto.PaymentMethod.Trim(),
                Status = "Open",
                UrgencyOrPreferredDate = dto.UrgencyOrPreferredDate?.Trim(),
                ExpectedDate = dto.ExpectedDate,
                CreatedAt = DateTime.UtcNow
            };

            // Add Job Areas
            if (dto.JobAreas != null && dto.JobAreas.Any())
            {
                foreach (var area in dto.JobAreas)
                {
                    job.JobAreas.Add(new UserJobArea
                    {
                        Id = Guid.NewGuid(),
                        CityName = area.CityName.Trim(),
                        Address = area.Address?.Trim(),
                        Latitude = area.Latitude,
                        Longitude = area.Longitude
                    });
                }
            }

            // Add Tags
            if (dto.TagIds != null && dto.TagIds.Any())
            {
                var validTagIds = await _context.Tags
                    .Where(t => dto.TagIds.Contains(t.Id))
                    .Select(t => t.Id)
                    .ToListAsync();

                foreach (var tagId in validTagIds)
                {
                    job.JobTags.Add(new UserJobTag
                    {
                        TagId = tagId
                    });
                }
            }

            await _context.UserJobs.AddAsync(job);
            await _context.SaveChangesAsync();

            return await GetJobByIdAsync(job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user job.");
            return ApiResponse<ReceiverJobDto>.Fail("Failed to create user job.");
        }
    }

    public async Task<ApiResponse<bool>> DeleteJobAsync(Guid userId, Guid jobId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("User not found.");
            }

            var job = await _context.UserJobs
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                return ApiResponse<bool>.Fail("Job not found.");
            }

            var isOwner = job.UserId == userId;
            var isAdmin = user.Role == UserRole.SuperAdmin || user.Role == UserRole.Admin;

            if (!isOwner && !isAdmin)
            {
                return ApiResponse<bool>.Fail("You are not authorized to delete this job.");
            }

            _context.UserJobs.Remove(job);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Job deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user job {JobId}.", jobId);
            return ApiResponse<bool>.Fail("Failed to delete user job.");
        }
    }

    public async Task<ApiResponse<List<ReceiverJobDto>>> SearchJobsAsync(JobSearchFilterDto filter)
    {
        try
        {
            var query = _context.UserJobs
                .Include(j => j.User)
                .Include(j => j.ServiceVariant)
                    .ThenInclude(v => v!.Service)
                        .ThenInclude(s => s!.Category)
                .Include(j => j.JobAreas)
                .Include(j => j.JobTags)
                    .ThenInclude(jt => jt.Tag)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(j => j.Status == filter.Status);
            }
            else
            {
                query = query.Where(j => j.Status == "Open");
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(j => j.ServiceVariant != null &&
                                         j.ServiceVariant.Service != null &&
                                         j.ServiceVariant.Service.CategoryId == filter.CategoryId.Value);
            }

            if (filter.ServiceId.HasValue)
            {
                query = query.Where(j => j.ServiceVariant != null &&
                                         j.ServiceVariant.ServiceId == filter.ServiceId.Value);
            }

            if (filter.VariantId.HasValue)
            {
                query = query.Where(j => j.ServiceVariantId == filter.VariantId.Value);
            }

            if (filter.MaxBudget.HasValue)
            {
                query = query.Where(j => j.Budget == null || j.Budget <= filter.MaxBudget.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Location))
            {
                var loc = filter.Location.Trim().ToLower();
                query = query.Where(j => j.JobAreas.Any(a => a.CityName.ToLower().Contains(loc)));
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(j =>
                    j.Title.ToLower().Contains(term) ||
                    (j.Description != null && j.Description.ToLower().Contains(term)) ||
                    (j.ServiceVariant != null && j.ServiceVariant.Name.ToLower().Contains(term)) ||
                    j.JobTags.Any(jt => jt.Tag != null && jt.Tag.Name.ToLower().Contains(term)));
            }

            if (filter.Tags != null && filter.Tags.Any())
            {
                var lowerTags = filter.Tags.Select(t => t.ToLower()).ToList();
                query = query.Where(j => j.JobTags.Any(jt => jt.Tag != null && lowerTags.Contains(jt.Tag.Name.ToLower())));
            }

            var jobs = await query
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new ReceiverJobDto
                {
                    Id = j.Id,
                    ReceiverProfileId = Guid.Empty,
                    UserId = j.UserId,
                    ReceiverName = j.User != null ? j.User.FullName : string.Empty,
                    ReceiverPhone = j.User != null ? j.User.PhoneNumber : null,
                    ServiceVariantId = j.ServiceVariantId,
                    ServiceVariantName = j.ServiceVariant != null ? j.ServiceVariant.Name : null,
                    ServiceName = j.ServiceVariant != null && j.ServiceVariant.Service != null ? j.ServiceVariant.Service.Name : null,
                    CategoryName = j.ServiceVariant != null && j.ServiceVariant.Service != null && j.ServiceVariant.Service.Category != null ? j.ServiceVariant.Service.Category.Name : null,
                    Title = j.Title,
                    Description = j.Description,
                    Budget = j.Budget,
                    PaymentMethod = j.PaymentMethod ?? "Any",
                    Status = j.Status,
                    UrgencyOrPreferredDate = j.UrgencyOrPreferredDate,
                    ExpectedDate = j.ExpectedDate,
                    CreatedAt = j.CreatedAt,
                    UpdatedAt = j.UpdatedAt,
                    JobAreas = j.JobAreas.Select(ja => new ReceiverJobAreaDto
                    {
                        Id = ja.Id,
                        CityName = ja.CityName,
                        Address = ja.Address,
                        Latitude = ja.Latitude,
                        Longitude = ja.Longitude
                    }).ToList(),
                    Tags = j.JobTags.Where(jt => jt.Tag != null).Select(jt => jt.Tag!.Name).ToList()
                })
                .ToListAsync();

            return ApiResponse<List<ReceiverJobDto>>.Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching user jobs.");
            return ApiResponse<List<ReceiverJobDto>>.Fail("Failed to search user jobs.");
        }
    }

    public async Task<ApiResponse<ReceiverJobDto>> GetJobByIdAsync(Guid id)
    {
        try
        {
            var job = await _context.UserJobs
                .Where(j => j.Id == id)
                .Include(j => j.User)
                .Include(j => j.ServiceVariant)
                    .ThenInclude(v => v!.Service)
                        .ThenInclude(s => s!.Category)
                .Include(j => j.JobAreas)
                .Include(j => j.JobTags)
                    .ThenInclude(jt => jt.Tag)
                .Select(j => new ReceiverJobDto
                {
                    Id = j.Id,
                    ReceiverProfileId = Guid.Empty,
                    UserId = j.UserId,
                    ReceiverName = j.User != null ? j.User.FullName : string.Empty,
                    ReceiverPhone = j.User != null ? j.User.PhoneNumber : null,
                    ServiceVariantId = j.ServiceVariantId,
                    ServiceVariantName = j.ServiceVariant != null ? j.ServiceVariant.Name : null,
                    ServiceName = j.ServiceVariant != null && j.ServiceVariant.Service != null ? j.ServiceVariant.Service.Name : null,
                    CategoryName = j.ServiceVariant != null && j.ServiceVariant.Service != null && j.ServiceVariant.Service.Category != null ? j.ServiceVariant.Service.Category.Name : null,
                    Title = j.Title,
                    Description = j.Description,
                    Budget = j.Budget,
                    PaymentMethod = j.PaymentMethod ?? "Any",
                    Status = j.Status,
                    UrgencyOrPreferredDate = j.UrgencyOrPreferredDate,
                    ExpectedDate = j.ExpectedDate,
                    CreatedAt = j.CreatedAt,
                    UpdatedAt = j.UpdatedAt,
                    JobAreas = j.JobAreas.Select(ja => new ReceiverJobAreaDto
                    {
                        Id = ja.Id,
                        CityName = ja.CityName,
                        Address = ja.Address,
                        Latitude = ja.Latitude,
                        Longitude = ja.Longitude
                    }).ToList(),
                    Tags = j.JobTags.Where(jt => jt.Tag != null).Select(jt => jt.Tag!.Name).ToList()
                })
                .FirstOrDefaultAsync();

            if (job == null)
            {
                return ApiResponse<ReceiverJobDto>.Fail("User job not found.");
            }

            return ApiResponse<ReceiverJobDto>.Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user job {JobId}.", id);
            return ApiResponse<ReceiverJobDto>.Fail("Failed to retrieve user job.");
        }
    }

    public async Task<ApiResponse<List<ReceiverJobDto>>> GetUserJobsAsync(Guid userId)
    {
        try
        {
            var jobs = await _context.UserJobs
                .Where(j => j.UserId == userId)
                .Include(j => j.User)
                .Include(j => j.ServiceVariant)
                    .ThenInclude(v => v!.Service)
                        .ThenInclude(s => s!.Category)
                .Include(j => j.JobAreas)
                .Include(j => j.JobTags)
                    .ThenInclude(jt => jt.Tag)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new ReceiverJobDto
                {
                    Id = j.Id,
                    ReceiverProfileId = Guid.Empty,
                    UserId = j.UserId,
                    ReceiverName = j.User != null ? j.User.FullName : string.Empty,
                    ReceiverPhone = j.User != null ? j.User.PhoneNumber : null,
                    ServiceVariantId = j.ServiceVariantId,
                    ServiceVariantName = j.ServiceVariant != null ? j.ServiceVariant.Name : null,
                    ServiceName = j.ServiceVariant != null && j.ServiceVariant.Service != null ? j.ServiceVariant.Service.Name : null,
                    CategoryName = j.ServiceVariant != null && j.ServiceVariant.Service != null && j.ServiceVariant.Service.Category != null ? j.ServiceVariant.Service.Category.Name : null,
                    Title = j.Title,
                    Description = j.Description,
                    Budget = j.Budget,
                    PaymentMethod = j.PaymentMethod ?? "Any",
                    Status = j.Status,
                    UrgencyOrPreferredDate = j.UrgencyOrPreferredDate,
                    ExpectedDate = j.ExpectedDate,
                    CreatedAt = j.CreatedAt,
                    UpdatedAt = j.UpdatedAt,
                    JobAreas = j.JobAreas.Select(ja => new ReceiverJobAreaDto
                    {
                        Id = ja.Id,
                        CityName = ja.CityName,
                        Address = ja.Address,
                        Latitude = ja.Latitude,
                        Longitude = ja.Longitude
                    }).ToList(),
                    Tags = j.JobTags.Where(jt => jt.Tag != null).Select(jt => jt.Tag!.Name).ToList()
                })
                .ToListAsync();

            return ApiResponse<List<ReceiverJobDto>>.Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs for user {UserId}.", userId);
            return ApiResponse<List<ReceiverJobDto>>.Fail("Failed to retrieve user jobs.");
        }
    }

    #endregion
}
