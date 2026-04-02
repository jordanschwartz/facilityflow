using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FacilityFlow.Core.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FacilityFlow.Infrastructure.Services;

public class GeminiGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GeminiGeocodingService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiGeocodingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiGeocodingService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _logger = logger;
        _cache = cache;
    }

    public async Task<(double Latitude, double Longitude)?> GeocodeZipAsync(string zip)
    {
        if (string.IsNullOrWhiteSpace(zip))
            return null;

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Cannot geocode ZIP.");
            return null;
        }

        var cacheKey = $"geocode-zip:{zip}";
        if (_cache.TryGetValue(cacheKey, out (double Latitude, double Longitude)? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var prompt = $"What is the latitude and longitude of the center of US ZIP code {zip}? Return JSON: {{\"latitude\": number, \"longitude\": number}}";

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
                _logger.LogWarning("Gemini API returned {StatusCode} for ZIP geocoding", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(json, JsonOptions);

            var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Gemini returned empty content for ZIP geocoding.");
                return null;
            }

            var result = JsonSerializer.Deserialize<GeocodingResult>(text, JsonOptions);
            if (result is null)
                return null;

            var coords = (result.Latitude, result.Longitude);
            _cache.Set(cacheKey, ((double, double)?)coords, CacheDuration);
            return coords;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Gemini API request timed out for ZIP geocoding.");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini geocoding response.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Gemini API for geocoding.");
            return null;
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

    private class GeocodingResult
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }
}
