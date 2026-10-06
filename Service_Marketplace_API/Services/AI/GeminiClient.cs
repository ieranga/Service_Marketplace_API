using System.Text;
using System.Text.Json;

namespace Service_Marketplace_API.Services.AI;

public class GeminiClient : IGeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GetApiKey());

    private string? GetApiKey()
    {
        var key = _configuration["Gemini:ApiKey"];
        if (!string.IsNullOrWhiteSpace(key))
            return key.Trim();

        return null;
    }

    public async Task<string?> GenerateStructuredJsonAsync(string systemPrompt, string userPrompt)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("Gemini API key is not configured. Falling back to local deterministic engine.");
            return null;
        }

        try
        {
            var modelId = _configuration["Gemini:ModelId"] ?? "gemini-3.8-flash";

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";

            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = userPrompt } }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.1
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(endpoint, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gemini API returned error code {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var textElement))
            {
                return textElement.GetString();
            }

            _logger.LogWarning("Unexpected Gemini response structure: {ResponseBody}", responseBody);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception calling Gemini API. Falling back to local matching.");
            return null;
        }
    }
}
