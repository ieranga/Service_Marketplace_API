using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Service_Marketplace_API.Data;
using Service_Marketplace_API.DTOs.AI;
using Service_Marketplace_API.DTOs.Common;
using Service_Marketplace_API.Entities;

namespace Service_Marketplace_API.Services.AI;

public class AIServiceDiscoveryService : IAIServiceDiscoveryService
{
    private readonly ApplicationDbContext _context;
    private readonly IGeminiClient _geminiClient;
    private readonly IMarketplaceMatchingService _matchingService;
    private readonly ILogger<AIServiceDiscoveryService> _logger;

    public AIServiceDiscoveryService(
        ApplicationDbContext context,
        IGeminiClient geminiClient,
        IMarketplaceMatchingService matchingService,
        ILogger<AIServiceDiscoveryService> logger)
    {
        _context = context;
        _geminiClient = geminiClient;
        _matchingService = matchingService;
        _logger = logger;
    }

    public async Task<ApiResponse<ServiceDiscoveryResponseDto>> DiscoverServicesAsync(ServiceDiscoveryRequestDto request)
    {
        try
        {
            var rawQuery = request.Query.Trim();
            if (string.IsNullOrWhiteSpace(rawQuery))
            {
                return ApiResponse<ServiceDiscoveryResponseDto>.Fail("Please enter a valid service request.");
            }

            // 1. Fetch current active marketplace catalog to ground the AI
            var catalog = await _context.Categories
                .Where(c => c.IsActive)
                .Include(c => c.Services.Where(s => s.IsActive))
                    .ThenInclude(s => s.Variants.Where(v => v.IsActive))
                .ToListAsync();

            var availableTags = await _context.Tags
                .Where(t => t.IsActive)
                .Select(t => t.Name)
                .ToListAsync();

            ExtractedRequirementsDto? extractedRequirements = null;
            string engineUsed = "Local-Catalog-Engine";

            // 2. Try Gemini API first if configured
            if (_geminiClient.IsConfigured)
            {
                var systemPrompt = BuildGroundingSystemPrompt(catalog, availableTags);
                var userPrompt = $"User Service Request: \"{rawQuery}\"";

                var jsonResult = await _geminiClient.GenerateStructuredJsonAsync(systemPrompt, userPrompt);
                if (!string.IsNullOrWhiteSpace(jsonResult))
                {
                    extractedRequirements = ParseAndValidateGeminiResponse(rawQuery, jsonResult, catalog, availableTags);
                    if (extractedRequirements != null)
                    {
                        engineUsed = "Google-Gemini-3.8-Flash";
                    }
                }
            }

            // 3. Fallback to intelligent deterministic catalog matching if Gemini is unconfigured or failed
            if (extractedRequirements == null)
            {
                extractedRequirements = PerformDeterministicCatalogMatch(rawQuery, catalog, availableTags);
                engineUsed = "Deterministic-Catalog-Fallback";
            }

            // Override location if user provided an explicit preference in request (ignoring Swagger placeholder "string")
            if (!string.IsNullOrWhiteSpace(request.PreferredLocation) && 
                !request.PreferredLocation.Trim().Equals("string", StringComparison.OrdinalIgnoreCase) && 
                string.IsNullOrWhiteSpace(extractedRequirements.Location))
            {
                extractedRequirements.Location = request.PreferredLocation.Trim();
            }

            // 4. Run matching engine
            var matchedProviders = await _matchingService.MatchProvidersAsync(extractedRequirements);

            var response = new ServiceDiscoveryResponseDto
            {
                ExtractedRequirements = extractedRequirements,
                MatchedProviders = matchedProviders,
                TotalMatches = matchedProviders.Count,
                EngineUsed = engineUsed,
                ProcessedAt = DateTime.UtcNow
            };

            return ApiResponse<ServiceDiscoveryResponseDto>.Ok(response, "Service discovery and provider matching completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI service discovery.");
            return ApiResponse<ServiceDiscoveryResponseDto>.Fail("An error occurred during service discovery. Please try again.");
        }
    }

    private static string BuildGroundingSystemPrompt(List<Category> catalog, List<string> tags)
    {
        var catalogSummary = new List<string>();
        foreach (var cat in catalog)
        {
            foreach (var svc in cat.Services)
            {
                foreach (var vr in svc.Variants)
                {
                    catalogSummary.Add($"- Category: \"{cat.Name}\" | Service: \"{svc.Name}\" | Variant: \"{vr.Name}\"");
                }
            }
        }

        var tagsList = string.Join(", ", tags.Select(t => $"\"{t}\""));

        return $@"You are the AI Service Discovery Agent for an AI-Powered Service Marketplace in Sri Lanka.
Your job is to understand the user's natural language request and extract structured requirements grounded STRICTLY in our authoritative marketplace catalog.

AUTHORITATIVE CATALOG HIERARCHY:
{string.Join("\n", catalogSummary)}

AVAILABLE SYSTEM TAGS:
[{tagsList}]

INSTRUCTIONS:
1. Identify the user intent.
2. Map the request to the MOST RELEVANT Category, Service, and Variant from the authoritative catalog above.
3. If no exact Variant matches, pick the closest one or leave Variant null.
4. Extract any matching tags from the available tags list (e.g. 'Home Visit', 'Mobile Service', 'Weekend', 'Emergency').
5. Extract the city/location in Sri Lanka (e.g. 'Negombo', 'Colombo', 'Katana', 'Ja-Ela', 'Kandy') if mentioned.
6. Extract the user's budget if mentioned (in numbers).
7. Extract urgency or date timing if mentioned.

You MUST respond ONLY with a valid JSON object matching this exact schema:
{{
  ""detectedIntent"": ""string"",
  ""categoryName"": ""string or null"",
  ""serviceName"": ""string or null"",
  ""variantName"": ""string or null"",
  ""extractedTags"": [""string""],
  ""location"": ""string or null"",
  ""maxBudget"": number or null,
  ""urgencyOrDate"": ""string or null"",
  ""confidenceScore"": number between 0.0 and 1.0
}}";
    }

    private ExtractedRequirementsDto? ParseAndValidateGeminiResponse(
        string rawQuery,
        string jsonText,
        List<Category> catalog,
        List<string> availableTags)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            var detectedIntent = root.TryGetProperty("detectedIntent", out var di) ? di.GetString() ?? "Service Request" : "Service Request";
            var categoryName = root.TryGetProperty("categoryName", out var cn) ? cn.GetString() : null;
            var serviceName = root.TryGetProperty("serviceName", out var sn) ? sn.GetString() : null;
            var variantName = root.TryGetProperty("variantName", out var vn) ? vn.GetString() : null;
            var location = root.TryGetProperty("location", out var loc) ? loc.GetString() : null;
            var urgency = root.TryGetProperty("urgencyOrDate", out var urg) ? urg.GetString() : null;

            decimal? maxBudget = null;
            if (root.TryGetProperty("maxBudget", out var mb) && mb.ValueKind == JsonValueKind.Number)
            {
                maxBudget = mb.GetDecimal();
            }

            double confidence = 0.9;
            if (root.TryGetProperty("confidenceScore", out var cs) && cs.ValueKind == JsonValueKind.Number)
            {
                confidence = cs.GetDouble();
            }

            var tags = new List<string>();
            if (root.TryGetProperty("extractedTags", out var et) && et.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in et.EnumerateArray())
                {
                    var tagStr = item.GetString();
                    if (!string.IsNullOrWhiteSpace(tagStr))
                    {
                        var matchingTag = availableTags.FirstOrDefault(t => t.Equals(tagStr, StringComparison.OrdinalIgnoreCase));
                        if (matchingTag != null) tags.Add(matchingTag);
                        else tags.Add(tagStr);
                    }
                }
            }

            // Backend Validation: Validate with DB system of record
            Category? matchedCategory = null;
            ServiceEntity? matchedService = null;
            ServiceVariant? matchedVariant = null;

            if (!string.IsNullOrWhiteSpace(variantName))
            {
                foreach (var cat in catalog)
                {
                    foreach (var s in cat.Services)
                    {
                        var v = s.Variants.FirstOrDefault(vr => vr.Name.Equals(variantName, StringComparison.OrdinalIgnoreCase) ||
                                                               vr.Name.ToLowerInvariant().Contains(variantName.ToLowerInvariant()));
                        if (v != null)
                        {
                            matchedCategory = cat;
                            matchedService = s;
                            matchedVariant = v;
                            break;
                        }
                    }
                    if (matchedVariant != null) break;
                }
            }

            if (matchedVariant == null && !string.IsNullOrWhiteSpace(serviceName))
            {
                foreach (var cat in catalog)
                {
                    var s = cat.Services.FirstOrDefault(svc => svc.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
                    if (s != null)
                    {
                        matchedCategory = cat;
                        matchedService = s;
                        matchedVariant = s.Variants.FirstOrDefault();
                        break;
                    }
                }
            }

            return new ExtractedRequirementsDto
            {
                RawQuery = rawQuery,
                DetectedIntent = detectedIntent,
                CategoryId = matchedCategory?.Id,
                CategoryName = matchedCategory?.Name ?? categoryName,
                ServiceId = matchedService?.Id,
                ServiceName = matchedService?.Name ?? serviceName,
                VariantId = matchedVariant?.Id,
                VariantName = matchedVariant?.Name ?? variantName,
                ExtractedTags = tags.Distinct().ToList(),
                Location = location,
                MaxBudget = maxBudget,
                UrgencyOrDate = urgency,
                ConfidenceScore = confidence,
                IsCatalogMatched = matchedVariant != null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini JSON output: {JsonText}", jsonText);
            return null;
        }
    }

    private static ExtractedRequirementsDto PerformDeterministicCatalogMatch(
        string rawQuery,
        List<Category> catalog,
        List<string> availableTags)
    {
        var lower = rawQuery.ToLowerInvariant();

        // 1. Detect tags
        var matchedTags = new List<string>();
        if (lower.Contains("home") || lower.Contains("doorstep") || lower.Contains("house")) matchedTags.Add("Home Visit");
        if (lower.Contains("mobile") || lower.Contains("van") || lower.Contains("on-site") || lower.Contains("onsite")) matchedTags.Add("Mobile Service");
        if (lower.Contains("weekend") || lower.Contains("saturday") || lower.Contains("sunday")) matchedTags.Add("Weekend");
        if (lower.Contains("urgent") || lower.Contains("emergency") || lower.Contains("quick") || lower.Contains("now")) matchedTags.Add("Emergency");
        if (lower.Contains("today") || lower.Contains("same day")) matchedTags.Add("Same Day Service");
        if (lower.Contains("certified") || lower.Contains("expert") || lower.Contains("qualified")) matchedTags.Add("Certified Technician");

        // 2. Detect common Sri Lankan locations
        string? location = null;
        var commonCities = new[] { "Negombo", "Katana", "Ja-Ela", "Kochchikade", "Katunayake", "Colombo", "Gampaha", "Kandy", "Wattala" };
        foreach (var city in commonCities)
        {
            if (lower.Contains(city.ToLowerInvariant()))
            {
                location = city;
                break;
            }
        }

        // 3. Search catalog for best matching Variant
        Category? matchedCat = null;
        ServiceEntity? matchedSvc = null;
        ServiceVariant? matchedVar = null;

        // Specific keyword heuristics for common services
        if (lower.Contains("car wash") || lower.Contains("wash my car") || lower.Contains("wash car") || lower.Contains("clean my car"))
        {
            FindCatalogHierarchy(catalog, "Transportation", "Vehicle Cleaning", "Car Washing", out matchedCat, out matchedSvc, out matchedVar);
        }
        else if (lower.Contains("laptop") || lower.Contains("notebook repair") || lower.Contains("screen broken"))
        {
            FindCatalogHierarchy(catalog, "Mechanical", "Computer Repair", "Laptop Repair", out matchedCat, out matchedSvc, out matchedVar);
        }
        else if (lower.Contains("pc") || lower.Contains("desktop") || lower.Contains("computer"))
        {
            FindCatalogHierarchy(catalog, "Mechanical", "Computer Repair", "Desktop PC Troubleshooting", out matchedCat, out matchedSvc, out matchedVar);
        }
        else if (lower.Contains("house clean") || lower.Contains("clean my house") || lower.Contains("deep clean"))
        {
            FindCatalogHierarchy(catalog, "Cleaning", "House Cleaning", "Deep House Cleaning", out matchedCat, out matchedSvc, out matchedVar);
        }
        else if (lower.Contains("grass") || lower.Contains("lawn") || lower.Contains("garden"))
        {
            FindCatalogHierarchy(catalog, "Cleaning", "Garden Cleaning", "Grass Cutting", out matchedCat, out matchedSvc, out matchedVar);
        }
        else if (lower.Contains("software") || lower.Contains("developer") || lower.Contains("website") || lower.Contains("app development") || lower.Contains("web development"))
        {
            FindCatalogHierarchy(catalog, "Digital & Professional", "Software Development", "Web Application Development", out matchedCat, out matchedSvc, out matchedVar);
        }
        else
        {
            // General scan across all variants (filtering out generic stop words like 'service', 'repair')
            var stopWords = new HashSet<string> { "service", "services", "repair", "repairs", "need", "needs", "only", "please", "want", "someone" };
            foreach (var cat in catalog)
            {
                foreach (var s in cat.Services)
                {
                    foreach (var v in s.Variants)
                    {
                        var vLower = v.Name.ToLowerInvariant();
                        var meaningfulWords = vLower.Split(' ')
                            .Where(w => w.Length > 3 && !stopWords.Contains(w))
                            .ToList();

                        if (lower.Contains(vLower) || (meaningfulWords.Any() && meaningfulWords.All(w => lower.Contains(w))))
                        {
                            matchedCat = cat;
                            matchedSvc = s;
                            matchedVar = v;
                            break;
                        }
                    }
                    if (matchedVar != null) break;
                }
                if (matchedVar != null) break;
            }
        }

        return new ExtractedRequirementsDto
        {
            RawQuery = rawQuery,
            DetectedIntent = matchedVar != null ? $"{matchedVar.Name} Request" : "General Service Search",
            CategoryId = matchedCat?.Id,
            CategoryName = matchedCat?.Name,
            ServiceId = matchedSvc?.Id,
            ServiceName = matchedSvc?.Name,
            VariantId = matchedVar?.Id,
            VariantName = matchedVar?.Name,
            ExtractedTags = matchedTags,
            Location = location,
            ConfidenceScore = matchedVar != null ? 0.85 : 0.50,
            IsCatalogMatched = matchedVar != null
        };
    }

    private static void FindCatalogHierarchy(
        List<Category> catalog,
        string categoryName,
        string serviceName,
        string variantName,
        out Category? cat,
        out ServiceEntity? svc,
        out ServiceVariant? vr)
    {
        cat = catalog.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        svc = cat?.Services.FirstOrDefault(s => s.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        vr = svc?.Variants.FirstOrDefault(v => v.Name.Equals(variantName, StringComparison.OrdinalIgnoreCase));
    }
}
