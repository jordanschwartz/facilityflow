using System.Net.Http.Json;
using System.Text.Json;
using FacilityFlow.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace FacilityFlow.Infrastructure.Services;

public class AiSummaryService : IAiSummaryService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    private const string SystemPrompt =
        "You are a professional facilities management company writing a proposal to a client. " +
        "Write a clean, professional summary of the proposed work. " +
        "Never mention subcontractors, vendors, or third parties. " +
        "Write as if your company is performing the work directly. " +
        "Be clear, confident, and client-friendly. Keep it concise (2-4 paragraphs).";

    public AiSummaryService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException(
                "OpenAI API key is not configured. Set 'OpenAI:ApiKey' in your configuration.");
    }

    public async Task<string> GenerateProposalSummaryAsync(
        string scopeOfWork, string? notes, string? jobDescription, string? additionalContext)
    {
        var userContent = $"Scope of Work:\n{scopeOfWork}";
        if (!string.IsNullOrWhiteSpace(notes))
            userContent += $"\n\nAdditional Notes:\n{notes}";
        if (!string.IsNullOrWhiteSpace(jobDescription))
            userContent += $"\n\nJob Description:\n{jobDescription}";
        if (!string.IsNullOrWhiteSpace(additionalContext))
            userContent += $"\n\nAdditional Context:\n{additionalContext}";

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userContent }
            },
            temperature = 0.7,
            max_tokens = 1000
        });

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var summary = json
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return summary ?? throw new InvalidOperationException("OpenAI returned an empty response.");
    }
}
