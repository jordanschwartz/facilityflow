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
        "You are writing a proposal summary for a facilities management company to present to a client. " +
        "Be factual, direct, and specific about what the job entails. " +
        "State exactly what work will be performed, the timeline, and any financial details provided. " +
        "If photos or images are referenced, mention that supporting documentation/photos are attached for review. " +
        "If a not-to-exceed price is provided, clearly state the pricing cap. " +
        "If start dates or estimated duration are provided, include the schedule. " +
        "If assumptions or exclusions are listed, include them as clear bullet points. " +
        "Never mention subcontractors, vendors, or third parties — write as if your company performs the work directly. " +
        "Keep it concise (2-4 paragraphs) and professional.";

    public AiSummaryService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException(
                "OpenAI API key is not configured. Set 'OpenAI:ApiKey' in your configuration.");
    }

    public async Task<string> GenerateProposalSummaryAsync(ProposalSummaryContext context)
    {
        var userContent = $"Scope of Work:\n{context.ScopeOfWork}";

        if (context.NotToExceedPrice.HasValue)
            userContent += $"\n\nNot-to-Exceed Price: ${context.NotToExceedPrice.Value:N2}";

        if (context.ProposedStartDate.HasValue)
            userContent += $"\n\nProposed Start Date: {context.ProposedStartDate.Value:MMMM d, yyyy}";

        if (context.EstimatedDurationValue.HasValue && !string.IsNullOrWhiteSpace(context.EstimatedDurationUnit))
            userContent += $"\n\nEstimated Duration: {context.EstimatedDurationValue.Value} {context.EstimatedDurationUnit}";

        if (!string.IsNullOrWhiteSpace(context.Assumptions))
            userContent += $"\n\nAssumptions:\n{context.Assumptions}";

        if (!string.IsNullOrWhiteSpace(context.Exclusions))
            userContent += $"\n\nExclusions:\n{context.Exclusions}";

        if (context.AttachmentFilenames.Count > 0)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".heic" };
            var imageFiles = context.AttachmentFilenames
                .Where(f => imageExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var otherFiles = context.AttachmentFilenames.Except(imageFiles).ToList();

            if (imageFiles.Count > 0)
                userContent += $"\n\n{imageFiles.Count} photo(s) are attached showing the job site/conditions.";
            if (otherFiles.Count > 0)
                userContent += $"\n\n{otherFiles.Count} supporting document(s) attached.";
        }

        if (!string.IsNullOrWhiteSpace(context.Notes))
            userContent += $"\n\nAdditional Notes:\n{context.Notes}";
        if (!string.IsNullOrWhiteSpace(context.JobDescription))
            userContent += $"\n\nJob Description:\n{context.JobDescription}";
        if (!string.IsNullOrWhiteSpace(context.AdditionalContext))
            userContent += $"\n\nAdditional Context:\n{context.AdditionalContext}";

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
            temperature = 0.5,
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
