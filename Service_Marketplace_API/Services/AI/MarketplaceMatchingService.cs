using Microsoft.EntityFrameworkCore;
using Service_Marketplace_API.Data;
using Service_Marketplace_API.DTOs.AI;
using Service_Marketplace_API.Entities;

namespace Service_Marketplace_API.Services.AI;

public class MarketplaceMatchingService : IMarketplaceMatchingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MarketplaceMatchingService> _logger;

    public MarketplaceMatchingService(ApplicationDbContext context, ILogger<MarketplaceMatchingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MatchedProviderDto>> MatchProvidersAsync(ExtractedRequirementsDto requirements)
    {
        try
        {
            // 1. Base query for active provider listings
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

            // Broad filter: if we have Category/Service/Variant, restrict candidates
            if (requirements.VariantId.HasValue)
            {
                query = query.Where(ps => ps.ServiceVariantId == requirements.VariantId.Value);
            }
            else if (requirements.ServiceId.HasValue)
            {
                query = query.Where(ps => ps.ServiceVariant != null && ps.ServiceVariant.ServiceId == requirements.ServiceId.Value);
            }
            else if (requirements.CategoryId.HasValue)
            {
                query = query.Where(ps => ps.ServiceVariant != null && ps.ServiceVariant.Service != null && ps.ServiceVariant.Service.CategoryId == requirements.CategoryId.Value);
            }

            var candidates = await query.ToListAsync();

            var normalizedLocation = requirements.Location?.Trim().ToLowerInvariant();
            var targetTags = requirements.ExtractedTags.Select(t => t.Trim().ToLowerInvariant()).ToList();

            var results = new List<MatchedProviderDto>();

            foreach (var ps in candidates)
            {
                var score = 0.0;
                var explanationParts = new List<string>();

                var provider = ps.ProviderServiceProfile?.User?.FullName ?? "Service Provider";
                var variantName = ps.ServiceVariant?.Name ?? "Service";
                var categoryName = ps.ServiceVariant?.Service?.Category?.Name ?? "General";
                var serviceName = ps.ServiceVariant?.Service?.Name ?? "Service";

                // 1. Variant Exact Match
                if (requirements.VariantId.HasValue && ps.ServiceVariantId == requirements.VariantId.Value)
                {
                    score += 40.0;
                    explanationParts.Add($"specializes in {variantName}");
                }
                else if (requirements.ServiceId.HasValue && ps.ServiceVariant?.ServiceId == requirements.ServiceId.Value)
                {
                    score += 25.0;
                }

                // 2. Location Area Match
                var coveredAreas = ps.ProviderServiceProfile?.ServiceAreas.Select(sa => sa.CityName).ToList() ?? new List<string>();
                var locationMatched = false;

                if (!string.IsNullOrWhiteSpace(normalizedLocation))
                {
                    locationMatched = coveredAreas.Any(ca => ca.ToLowerInvariant().Contains(normalizedLocation));
                    if (locationMatched)
                    {
                        score += 30.0;
                        explanationParts.Add($"covers {requirements.Location}");
                    }
                    else
                    {
                        // Minor penalty if specific location requested and provider does not list it
                        score -= 10.0;
                    }
                }
                else
                {
                    score += 15.0; // No specific location required
                }

                // 3. Tag Overlap Match
                var providerTags = ps.ProviderServiceTags.Where(pst => pst.Tag != null).Select(pst => pst.Tag!.Name).ToList();
                var matchedTags = new List<string>();

                foreach (var tag in providerTags)
                {
                    if (targetTags.Any(tt => tt.Equals(tag, StringComparison.OrdinalIgnoreCase) || tag.ToLowerInvariant().Contains(tt)))
                    {
                        matchedTags.Add(tag);
                        score += 6.0;
                    }
                }

                if (matchedTags.Any())
                {
                    explanationParts.Add($"matches requested attributes ({string.Join(", ", matchedTags)})");
                }

                // 4. Verification Match
                var isVerified = ps.ProviderServiceProfile?.IsVerified ?? false;
                if (isVerified)
                {
                    score += 10.0;
                    explanationParts.Add("verified provider");
                }

                // 5. Rating & Experience
                var rating = ps.ProviderServiceProfile?.RatingAverage ?? 5.0;
                score += (rating / 5.0) * 10.0;

                // 6. Budget constraint check
                if (requirements.MaxBudget.HasValue && requirements.MaxBudget.Value > 0)
                {
                    if (ps.StartingPrice <= requirements.MaxBudget.Value)
                    {
                        score += 10.0;
                        explanationParts.Add($"within budget (Rs. {ps.StartingPrice:N0})");
                    }
                    else
                    {
                        score -= 15.0; // Above requested budget
                    }
                }

                // Normalize score between 0 and 100
                var finalScore = Math.Round(Math.Clamp(score, 10.0, 99.0), 1);

                // Build friendly explanation sentence
                var explanation = $"{provider} {string.Join(", ", explanationParts)}.";

                results.Add(new MatchedProviderDto
                {
                    ProviderServiceId = ps.Id,
                    ProviderServiceProfileId = ps.ProviderServiceProfileId,
                    ProviderName = provider,
                    BusinessName = ps.ProviderServiceProfile?.BusinessName,
                    IsVerified = isVerified,
                    RatingAverage = Math.Round(rating, 1),
                    ReviewCount = ps.ProviderServiceProfile?.ReviewCount ?? 0,
                    CategoryName = categoryName,
                    ServiceName = serviceName,
                    VariantName = variantName,
                    StartingPrice = ps.StartingPrice,
                    PriceUnit = ps.PriceUnit,
                    SupportsHomeVisit = ps.SupportsHomeVisit,
                    ServiceAreas = coveredAreas,
                    MatchedTags = matchedTags,
                    MatchScore = finalScore,
                    MatchExplanation = explanation
                });
            }

            return results.OrderByDescending(r => r.MatchScore).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching providers for requirements.");
            return new List<MatchedProviderDto>();
        }
    }
}
