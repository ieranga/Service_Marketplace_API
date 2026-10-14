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

            // Apply preferred location from request if not already identified from the natural language query
            if (string.IsNullOrWhiteSpace(extractedRequirements.Location) &&
                !string.IsNullOrWhiteSpace(request.PreferredLocation) && 
                !request.PreferredLocation.Trim().Equals("string", StringComparison.OrdinalIgnoreCase))
            {
                extractedRequirements.Location = request.PreferredLocation.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(request.PreferredLocation) && 
                     !request.PreferredLocation.Trim().Equals("string", StringComparison.OrdinalIgnoreCase) &&
                     !string.IsNullOrWhiteSpace(extractedRequirements.Location) &&
                     !rawQuery.Contains(extractedRequirements.Location, StringComparison.OrdinalIgnoreCase))
            {
                // If query didn't explicitly name the extracted location, prioritize user's explicit parameter
                extractedRequirements.Location = request.PreferredLocation.Trim();
            }

            // Override max budget if user provided explicit MaxBudget in request
            if (request.MaxBudget.HasValue && request.MaxBudget.Value > 0)
            {
                extractedRequirements.MaxBudget = request.MaxBudget.Value;
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
3. If the user asks for a general service (e.g. 'computer repair' or 'pet care'), map to that service and leave variant null unless a specific variant is mentioned.
4. Extract any matching tags from the available tags list (e.g. 'Home Visit', 'Mobile Service', 'Weekend', 'Emergency').
5. Extract the city/location in Sri Lanka (e.g. 'Rathnapura', 'Negombo', 'Colombo', 'Kandy') if mentioned.
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
                    var s = cat.Services.FirstOrDefault(svc => svc.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase) ||
                                                               svc.Name.ToLowerInvariant().Contains(serviceName.ToLowerInvariant()));
                    if (s != null)
                    {
                        matchedCategory = cat;
                        matchedService = s;
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
                IsCatalogMatched = matchedService != null || matchedVariant != null
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
        if (lower.Contains("home") || lower.Contains("doorstep") || lower.Contains("house") || lower.Contains("visit")) matchedTags.Add("Home Visit");
        if (lower.Contains("mobile") || lower.Contains("van") || lower.Contains("on-site") || lower.Contains("onsite")) matchedTags.Add("Mobile Service");
        if (lower.Contains("weekend") || lower.Contains("saturday") || lower.Contains("sunday")) matchedTags.Add("Weekend");
        if (lower.Contains("urgent") || lower.Contains("emergency") || lower.Contains("quick") || lower.Contains("now")) matchedTags.Add("Emergency");
        if (lower.Contains("today") || lower.Contains("same day")) matchedTags.Add("Same Day Service");
        if (lower.Contains("certified") || lower.Contains("expert") || lower.Contains("qualified")) matchedTags.Add("Certified Technician");

        // 2. Detect Sri Lankan locations
        string? location = null;
        var commonCities = new[]
        {
            "Rathnapura", "Ratnapura", "Colombo", "Negombo", "Kandy", "Galle", "Matara",
            "Kurunegala", "Gampaha", "Kalutara", "Anuradhapura", "Jaffna", "Badulla",
            "Nuwara Eliya", "Kegalle", "Trincomalee", "Batticaloa", "Matale", "Puttalam",
            "Ampara", "Hambantota", "Monaragala", "Polonnaruwa", "Mannar", "Vavuniya",
            "Kilinochchi", "Mullaitivu", "Katana", "Ja-Ela", "Katunayake", "Kochchikade",
            "Wattala", "Dehiwala", "Moratuwa", "Panadura", "Mount Lavinia", "Maharagama",
            "Kottawa", "Nugegoda", "Malabe", "Battaramulla", "Pelmadulla", "Kuruwita", "Balangoda"
        };

        foreach (var city in commonCities)
        {
            if (lower.Contains(city.ToLowerInvariant()))
            {
                location = city.Equals("Ratnapura", StringComparison.OrdinalIgnoreCase) ? "Rathnapura" : city;
                break;
            }
        }

        // Also check regex pattern for "near <City>" or "in <City>"
        if (string.IsNullOrWhiteSpace(location))
        {
            var locMatch = System.Text.RegularExpressions.Regex.Match(lower, @"(?:near|in|around|at)\s+([a-zA-Z\s\-]+?)(?:\s+(?:with|for|under|below|budget|and)|$)");
            if (locMatch.Success)
            {
                var candidate = locMatch.Groups[1].Value.Trim();
                if (candidate.Length > 2 && !candidate.Equals("me", StringComparison.OrdinalIgnoreCase))
                {
                    location = candidate;
                }
            }
        }

        // 3. Extract Budget parameter from natural language
        decimal? maxBudget = null;
        var budgetMatch = System.Text.RegularExpressions.Regex.Match(lower, @"(?:budget|price|max|under|below|rs\.?|lkr)\s*[:=]?\s*([0-9]{3,7})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!budgetMatch.Success)
        {
            budgetMatch = System.Text.RegularExpressions.Regex.Match(lower, @"([0-9]{3,7})\s*(?:lkr|rs|rupees)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        if (budgetMatch.Success && decimal.TryParse(budgetMatch.Groups[1].Value, out var parsedBudget))
        {
            maxBudget = parsedBudget;
        }

        // 4. Match Catalog Category, Service, and Variant
        Category? matchedCat = null;
        ServiceEntity? matchedSvc = null;
        ServiceVariant? matchedVar = null;

        // Computer Repair
        if (lower.Contains("computer") || lower.Contains("laptop") || lower.Contains("pc") || lower.Contains("desktop") || lower.Contains("windows installation"))
        {
            FindCatalogHierarchy(catalog, "Mechanical", "Computer Repair", null, out matchedCat, out matchedSvc, out _);
            if (lower.Contains("laptop") || lower.Contains("notebook"))
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Laptop", StringComparison.OrdinalIgnoreCase));
            else if (lower.Contains("desktop") || lower.Contains("pc troubleshoot"))
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Desktop", StringComparison.OrdinalIgnoreCase));
            else if (lower.Contains("os") || lower.Contains("windows") || lower.Contains("software install"))
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("OS", StringComparison.OrdinalIgnoreCase));
        }
        // Pet Care
        else if (lower.Contains("pet") || lower.Contains("dog") || lower.Contains("cat") || lower.Contains("puppy") || lower.Contains("kitten") || lower.Contains("grooming") || lower.Contains("pet sitting"))
        {
            FindCatalogHierarchy(catalog, "Caring", "Pet Care", null, out matchedCat, out matchedSvc, out _);
            if (lower.Contains("groom") || lower.Contains("bath") || lower.Contains("fur"))
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Grooming", StringComparison.OrdinalIgnoreCase));
            else if (lower.Contains("sitting") || lower.Contains("board") || lower.Contains("walk"))
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Sitting", StringComparison.OrdinalIgnoreCase));
        }
        // Vehicle Cleaning / Car Wash
        else if (lower.Contains("car wash") || lower.Contains("wash my car") || lower.Contains("wash car") || lower.Contains("vehicle clean") || lower.Contains("detailing"))
        {
            FindCatalogHierarchy(catalog, "Transportation", "Vehicle Cleaning", null, out matchedCat, out matchedSvc, out _);
            if (lower.Contains("detail"))
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Detailing", StringComparison.OrdinalIgnoreCase));
            else if (lower.Contains("bike") || lower.Contains("motorbike"))
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Motorbike", StringComparison.OrdinalIgnoreCase));
            else
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Car Washing", StringComparison.OrdinalIgnoreCase));
        }
        // House Cleaning
        else if (lower.Contains("house clean") || lower.Contains("clean my house") || lower.Contains("deep clean") || lower.Contains("sofa") || lower.Contains("carpet"))
        {
            FindCatalogHierarchy(catalog, "Cleaning", "House Cleaning", null, out matchedCat, out matchedSvc, out _);
            if (lower.Contains("sofa") || lower.Contains("carpet"))
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Sofa", StringComparison.OrdinalIgnoreCase));
            else
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Deep", StringComparison.OrdinalIgnoreCase));
        }
        // Garden Cleaning
        else if (lower.Contains("grass") || lower.Contains("lawn") || lower.Contains("garden") || lower.Contains("tree"))
        {
            FindCatalogHierarchy(catalog, "Cleaning", "Garden Cleaning", null, out matchedCat, out matchedSvc, out _);
            if (lower.Contains("tree"))
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Tree", StringComparison.OrdinalIgnoreCase));
            else
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Grass", StringComparison.OrdinalIgnoreCase));
        }
        // Meal Catering
        else if (lower.Contains("food") || lower.Contains("meal") || lower.Contains("catering") || lower.Contains("lunch"))
        {
            FindCatalogHierarchy(catalog, "Food Preparation", "Meal Catering", null, out matchedCat, out matchedSvc, out _);
            if (lower.Contains("lunch") || lower.Contains("pack"))
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Lunch", StringComparison.OrdinalIgnoreCase));
            else
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Party", StringComparison.OrdinalIgnoreCase));
        }
        // Software Development
        else if (lower.Contains("software") || lower.Contains("developer") || lower.Contains("website") || lower.Contains("app development") || lower.Contains("web development"))
        {
            FindCatalogHierarchy(catalog, "Digital & Professional", "Software Development", null, out matchedCat, out matchedSvc, out _);
            if (lower.Contains("wordpress"))
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("WordPress", StringComparison.OrdinalIgnoreCase));
            else
                matchedVar = matchedSvc?.Variants.FirstOrDefault(v => v.Name.Contains("Web", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            // General scan across all catalog services and variants
            foreach (var cat in catalog)
            {
                if (lower.Contains(cat.Name.ToLowerInvariant()))
                {
                    matchedCat = cat;
                }
                foreach (var s in cat.Services)
                {
                    if (lower.Contains(s.Name.ToLowerInvariant()))
                    {
                        matchedCat = cat;
                        matchedSvc = s;
                    }
                    foreach (var v in s.Variants)
                    {
                        if (lower.Contains(v.Name.ToLowerInvariant()))
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

        string detectedIntent;
        if (matchedVar != null) detectedIntent = $"{matchedVar.Name} Request";
        else if (matchedSvc != null) detectedIntent = $"{matchedSvc.Name} Request";
        else if (matchedCat != null) detectedIntent = $"{matchedCat.Name} Service Request";
        else detectedIntent = "General Marketplace Service Request";

        return new ExtractedRequirementsDto
        {
            RawQuery = rawQuery,
            DetectedIntent = detectedIntent,
            CategoryId = matchedCat?.Id,
            CategoryName = matchedCat?.Name,
            ServiceId = matchedSvc?.Id,
            ServiceName = matchedSvc?.Name,
            VariantId = matchedVar?.Id,
            VariantName = matchedVar?.Name,
            ExtractedTags = matchedTags,
            Location = location,
            MaxBudget = maxBudget,
            ConfidenceScore = matchedVar != null ? 0.95 : (matchedSvc != null ? 0.90 : 0.70),
            IsCatalogMatched = matchedSvc != null || matchedVar != null
        };
    }

    private static void FindCatalogHierarchy(
        List<Category> catalog,
        string categoryName,
        string serviceName,
        string? variantName,
        out Category? cat,
        out ServiceEntity? svc,
        out ServiceVariant? vr)
    {
        cat = catalog.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        svc = cat?.Services.FirstOrDefault(s => s.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        vr = !string.IsNullOrWhiteSpace(variantName)
            ? svc?.Variants.FirstOrDefault(v => v.Name.Equals(variantName, StringComparison.OrdinalIgnoreCase))
            : null;
    }
}
