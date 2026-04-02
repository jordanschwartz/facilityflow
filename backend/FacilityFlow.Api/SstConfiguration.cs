using System.Text.Json;

namespace FacilityFlow.Api;

/// <summary>
/// Reads SST resource-linked environment variables and maps them into IConfiguration.
/// SST injects SST_RESOURCE_{Name} env vars as JSON blobs.
/// </summary>
public static class SstConfiguration
{
    public static void ConfigureFromSst(this WebApplicationBuilder builder)
    {
        var overrides = new Dictionary<string, string?>();

        // Database: SST_RESOURCE_Database → ConnectionStrings:DefaultConnection
        var dbJson = Environment.GetEnvironmentVariable("SST_RESOURCE_Database");
        if (!string.IsNullOrEmpty(dbJson))
        {
            var db = JsonSerializer.Deserialize<JsonElement>(dbJson);
            var host = db.GetProperty("host").GetString();
            var port = db.GetProperty("port").GetInt32();
            var username = db.GetProperty("username").GetString();
            var password = db.GetProperty("password").GetString();
            var database = db.GetProperty("database").GetString();
            overrides["ConnectionStrings:DefaultConnection"] =
                $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        }

        // JWT Secret
        var jwtJson = Environment.GetEnvironmentVariable("SST_RESOURCE_JwtSecret");
        if (!string.IsNullOrEmpty(jwtJson))
        {
            var secret = JsonSerializer.Deserialize<JsonElement>(jwtJson);
            overrides["Jwt:Secret"] = secret.GetProperty("value").GetString();
        }

        // Stripe Secret Key
        var stripeKeyJson = Environment.GetEnvironmentVariable("SST_RESOURCE_StripeSecretKey");
        if (!string.IsNullOrEmpty(stripeKeyJson))
        {
            var secret = JsonSerializer.Deserialize<JsonElement>(stripeKeyJson);
            overrides["Stripe:SecretKey"] = secret.GetProperty("value").GetString();
        }

        // Stripe Webhook Secret
        var stripeWhJson = Environment.GetEnvironmentVariable("SST_RESOURCE_StripeWebhookSecret");
        if (!string.IsNullOrEmpty(stripeWhJson))
        {
            var secret = JsonSerializer.Deserialize<JsonElement>(stripeWhJson);
            overrides["Stripe:WebhookSecret"] = secret.GetProperty("value").GetString();
        }

        // Gemini API Key
        var geminiJson = Environment.GetEnvironmentVariable("SST_RESOURCE_GeminiApiKey");
        if (!string.IsNullOrEmpty(geminiJson))
        {
            var secret = JsonSerializer.Deserialize<JsonElement>(geminiJson);
            overrides["Gemini:ApiKey"] = secret.GetProperty("value").GetString();
        }

        if (overrides.Count > 0)
        {
            builder.Configuration.AddInMemoryCollection(overrides);
        }
    }
}
