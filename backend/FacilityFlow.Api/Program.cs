using System.Text;
using FacilityFlow.Api;
using FacilityFlow.Api.Middleware;
using FacilityFlow.Application;
using FacilityFlow.Infrastructure;
using FacilityFlow.Infrastructure.Persistence;
using FacilityFlow.Infrastructure.SeedData;
using FacilityFlow.Core.Enums;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- SST Resource Linking (reads SST_RESOURCE_* env vars in deployed environments) ----
builder.ConfigureFromSst();

// ---- Infrastructure + Application ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

// ---- CORS ----
var allowedOrigins = builder.Configuration.GetSection("App:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        if (builder.Environment.IsProduction())
        {
            // In production, allow any HTTPS origin (CloudFront domain assigned at deploy)
            // Tighten this to the specific domain once known
            policy
                .SetIsOriginAllowed(origin => new Uri(origin).Scheme == "https")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// ---- JWT Authentication ----
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ---- FluentValidation ----
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ---- Controllers ----
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ---- Swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FacilityFlow API",
        Version = "v1",
        Description = "Backend API for FacilityFlow facility management platform"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without 'Bearer ' prefix)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---- CORS (must be first) ----
app.UseCors("FrontendPolicy");

// ---- Static Files (uploads) ----
var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(wwwroot, "uploads"));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwroot),
    RequestPath = ""
});

// ---- Exception Handling ----
app.UseMiddleware<ExceptionHandlingMiddleware>();

// ---- Swagger ----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FacilityFlow API v1");
        options.RoutePrefix = "swagger";
    });
}

// ---- Auth ----
app.UseAuthentication();
app.UseAuthorization();

// ---- Health Check ----
app.MapGet("/", () => Results.Ok(new { status = "healthy" }));

// ---- Controllers ----
app.MapControllers();

// ---- Database Setup & Seeding ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();

        // One-time database reset (clears test data, keeps schema + admin)
        var resetEnabled = builder.Configuration.GetValue<bool>("App:ResetDatabase");
        if (resetEnabled)
        {
            await db.Database.ExecuteSqlRawAsync(@"
                TRUNCATE TABLE ""ActivityLogs"", ""Invoices"", ""Comments"", ""Notifications"",
                    ""ProposalVersions"", ""ProposalAttachments"", ""Proposals"",
                    ""QuoteLineItems"", ""Quotes"", ""VendorInvites"",
                    ""WorkOrders"", ""Attachments"", ""ServiceRequests"",
                    ""VendorNotes"", ""VendorPayments"", ""Vendors"",
                    ""Clients"", ""Users""
                CASCADE
            ");
        }

        var seedEnabled = builder.Configuration.GetValue<bool>("App:SeedDatabase");
        if (seedEnabled)
        {
            await DbSeeder.SeedAsync(db);
        }

        // Ensure admin user exists
        await AdminSeeder.SeedAdminAsync(db);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database migration/seeding.");
    }
}

app.Run();
