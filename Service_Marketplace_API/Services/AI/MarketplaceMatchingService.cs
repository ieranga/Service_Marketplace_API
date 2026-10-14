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

            // Budget filter if specified
            if (requirements.MaxBudget.HasValue && requirements.MaxBudget.Value > 0)
            {
                query = query.Where(ps => ps.StartingPrice <= requirements.MaxBudget.Value);
            }

            var candidates = await query.ToListAsync();

            // Fallback relaxation: If specific variant filter yielded no results, relax to parent service
            if (candidates.Count == 0 && requirements.VariantId.HasValue && requirements.ServiceId.HasValue)
            {
                var relaxedQuery = _context.ProviderServices
                    .Where(ps => ps.IsActive && ps.ServiceVariant != null && ps.ServiceVariant.ServiceId == requirements.ServiceId.Value)
                    .Include(ps => ps.ProviderServiceProfile).ThenInclude(p => p!.User)
                    .Include(ps => ps.ProviderServiceProfile).ThenInclude(p => p!.ServiceAreas)
                    .Include(ps => ps.ServiceVariant).ThenInclude(v => v!.Service).ThenInclude(s => s!.Category)
                    .Include(ps => ps.ProviderServiceTags).ThenInclude(pst => pst.Tag)
                    .AsQueryable();

                if (requirements.MaxBudget.HasValue && requirements.MaxBudget.Value > 0)
                {
                    relaxedQuery = relaxedQuery.Where(ps => ps.StartingPrice <= requirements.MaxBudget.Value);
                }

                candidates = await relaxedQuery.ToListAsync();
            }

            var normalizedLocation = requirements.Location?.Trim().ToLowerInvariant();
            if (normalizedLocation == "ratnapura") normalizedLocation = "rathnapura";

            var targetTags = requirements.ExtractedTags.Select(t => t.Trim().ToLowerInvariant()).ToList();
            var results = new List<MatchedProviderDto>();

            foreach (var ps in candidates)
            {
                var score = 40.0;
                var explanationParts = new List<string>();

                var provider = ps.ProviderServiceProfile?.User?.FullName ?? "Service Provider";
                var businessName = ps.ProviderServiceProfile?.BusinessName;
                var variantName = ps.ServiceVariant?.Name ?? "Service";
                var categoryName = ps.ServiceVariant?.Service?.Category?.Name ?? "General";
                var serviceName = ps.ServiceVariant?.Service?.Name ?? "Service";

                // 1. Variant / Service Match
                if (requirements.VariantId.HasValue && ps.ServiceVariantId == requirements.VariantId.Value)
                {
                    score += 20.0;
                    explanationParts.Add($"specializes in {variantName}");
                }
                else if (requirements.ServiceId.HasValue && ps.ServiceVariant?.ServiceId == requirements.ServiceId.Value)
                {
                    score += 15.0;
                    explanationParts.Add($"provides {serviceName} ({variantName})");
                }

                // 2. Location Area Match
                var coveredAreas = ps.ProviderServiceProfile?.ServiceAreas.Select(sa => sa.CityName).ToList() ?? new List<string>();
                var locationMatched = false;

                if (!string.IsNullOrWhiteSpace(normalizedLocation))
                {
                    locationMatched = coveredAreas.Any(ca => 
                    {
                        var caNorm = ca.ToLowerInvariant();
                        if (caNorm == "ratnapura") caNorm = "rathnapura";
                        return caNorm.Contains(normalizedLocation) || normalizedLocation.Contains(caNorm);
                    });

                    if (locationMatched)
                    {
                        score += 30.0;
                        explanationParts.Add($"covers {requirements.Location}");
                    }
                    else if (coveredAreas.Any(ca => ca.ToLowerInvariant().Contains("islandwide") || ca.ToLowerInvariant().Contains("all island")))
                    {
                        score += 15.0;
                        explanationParts.Add("offers Islandwide coverage");
                    }
                    else
                    {
                        score -= 5.0;
                        explanationParts.Add($"based in {string.Join(", ", coveredAreas)}");
                    }
                }
                else
                {
                    score += 10.0;
                }

                // 3. Tag Overlap Match
                var providerTags = ps.ProviderServiceTags.Where(pst => pst.Tag != null).Select(pst => pst.Tag!.Name).ToList();
                var matchedTags = new List<string>();

                foreach (var tag in providerTags)
                {
                    if (targetTags.Any(tt => tt.Equals(tag, StringComparison.OrdinalIgnoreCase) || tag.ToLowerInvariant().Contains(tt)))
                    {
                        matchedTags.Add(tag);
                        score += 5.0;
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
                    score += 5.0;
                    explanationParts.Add("verified provider");
                }

                // 5. Rating & Experience
                var rating = ps.ProviderServiceProfile?.RatingAverage ?? 5.0;
                score += (rating / 5.0) * 5.0;

                // 6. Budget constraint check
                if (requirements.MaxBudget.HasValue && requirements.MaxBudget.Value > 0)
                {
                    if (ps.StartingPrice <= requirements.MaxBudget.Value)
                    {
                        score += 10.0;
                        explanationParts.Add($"within budget (Rs. {ps.StartingPrice:N0})");
                    }
                }

                // Normalize score between 0.20 and 0.99
                var finalScore = Math.Round(Math.Clamp(score, 20.0, 99.0) / 100.0, 2);

                // Build friendly explanation sentence
                var explanation = $"{businessName ?? provider} {string.Join(", ", explanationParts)}.";

                results.Add(new MatchedProviderDto
                {
                    ProviderServiceId = ps.Id,
                    ProviderServiceProfileId = ps.ProviderServiceProfileId,
                    ProviderName = provider,
                    BusinessName = businessName,
                    IsVerified = isVerified,
                    RatingAverage = Math.Round(rating, 1),
                    ReviewCount = ps.ProviderServiceProfile?.ReviewCount ?? 0,
                    CompletedJobsCount = ps.ProviderServiceProfile?.CompletedJobsCount ?? 0,
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

            // PRIMARY SORTING ORDER:
            // 1. Location match (if location was requested, providers covering the location come first)
            // 2. StartingPrice ascending (lowest price first - "first 10 services based on price")
            // 3. MatchScore descending
            // 4. Take top 10
            IEnumerable<MatchedProviderDto> sortedResults;
            if (!string.IsNullOrWhiteSpace(normalizedLocation))
            {
                sortedResults = results
                    .OrderByDescending(r => r.ServiceAreas.Any(sa => 
                    {
                        var saNorm = sa.ToLowerInvariant();
                        if (saNorm == "ratnapura") saNorm = "rathnapura";
                        return saNorm.Contains(normalizedLocation) || normalizedLocation.Contains(saNorm);
                    }))
                    .ThenBy(r => r.StartingPrice)
                    .ThenByDescending(r => r.MatchScore);
            }
            else
            {
                sortedResults = results
                    .OrderBy(r => r.StartingPrice)
                    .ThenByDescending(r => r.MatchScore);
            }

            return sortedResults.Take(10).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching providers for requirements.");
            return new List<MatchedProviderDto>();
        }
    }
}
