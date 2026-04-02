using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FacilityFlow.Core.Interfaces.Services;
using FacilityFlow.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FacilityFlow.Infrastructure.Services;

public class GeminiVendorDiscoveryService : IVendorDiscoveryService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GeminiVendorDiscoveryService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiVendorDiscoveryService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiVendorDiscoveryService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _logger = logger;
        _cache = cache;
    }

    public async Task<List<DiscoveredVendor>> SearchAsync(string trade, string zip, int radiusMiles)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Returning empty results.");
            return [];
        }

        var cacheKey = $"vendor-discovery:{trade.ToLowerInvariant()}:{zip}:{radiusMiles}";
        if (_cache.TryGetValue(cacheKey, out List<DiscoveredVendor>? cached) && cached != null)
        {
            _logger.LogInformation("Returning cached vendor discovery results for {CacheKey}", cacheKey);
            return cached;
        }

        try
        {
            var prompt = $"""
                Find local {trade} businesses near ZIP code {zip} within a {radiusMiles}-mile radius.
                Return a JSON array of up to 10 results with these fields:
                - businessName (string, required)
                - address (string, required)
                - phone (string or null)
                - website (string or null)
                - rating (number or null, Google rating out of 5)
                - reviewCount (integer or null)
                - googleProfileUrl (string or null)

                Only return the JSON array, no other text.
                """;

            var requestBody = new GeminiRequest
            {
                Contents = [new GeminiContent { Parts = [new GeminiPart { Text = prompt }] }],
                GenerationConfig = new GeminiGenerationConfig { ResponseMimeType = "application/json" }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, JsonOptions, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API returned {StatusCode}", response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(json, JsonOptions);

            var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Gemini returned empty content.");
                return [];
            }

            var results = JsonSerializer.Deserialize<List<GeminiVendorResult>>(text, JsonOptions);
            if (results is null)
                return [];

            var vendors = results.Select(r => new DiscoveredVendor
            {
                BusinessName = r.BusinessName ?? string.Empty,
                Address = r.Address ?? string.Empty,
                Phone = r.Phone,
                Website = r.Website,
                Rating = r.Rating,
                ReviewCount = r.ReviewCount,
                GoogleProfileUrl = r.GoogleProfileUrl
            }).ToList();

            _cache.Set(cacheKey, vendors, CacheDuration);
            return vendors;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Gemini API request timed out.");
            return [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini API response.");
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Gemini API.");
            return [];
        }
    }

    // ── Gemini API request/response models ───────────────────────────────────

    private class GeminiRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = [];

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig GenerationConfig { get; set; } = new();
    }

    private class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private class GeminiPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private class GeminiGenerationConfig
    {
        [JsonPropertyName("responseMimeType")]
        public string ResponseMimeType { get; set; } = "application/json";
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    private class GeminiVendorResult
    {
        [JsonPropertyName("businessName")]
        public string? BusinessName { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("rating")]
        public decimal? Rating { get; set; }

        [JsonPropertyName("reviewCount")]
        public int? ReviewCount { get; set; }

        [JsonPropertyName("googleProfileUrl")]
        public string? GoogleProfileUrl { get; set; }
    }
}
