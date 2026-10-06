namespace Service_Marketplace_API.Services.AI;

public interface IGeminiClient
{
    bool IsConfigured { get; }
    Task<string?> GenerateStructuredJsonAsync(string systemPrompt, string userPrompt);
}
