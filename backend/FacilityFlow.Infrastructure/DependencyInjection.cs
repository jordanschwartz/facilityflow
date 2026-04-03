using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using FacilityFlow.Infrastructure.Persistence;
using FacilityFlow.Infrastructure.Repositories;
using FacilityFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FacilityFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var dataSource = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("DefaultConnection"))
            .EnableDynamicJson()
            .Build();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(dataSource)
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.NavigationBaseIncludeIgnored)));

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
        services.AddScoped<IProposalRepository, ProposalRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        // Caching
        services.AddMemoryCache();

        // Infrastructure services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddHttpClient<IAiSummaryService, AiSummaryService>();
        services.AddHttpClient<IVendorDiscoveryService, GeminiVendorDiscoveryService>();
        services.AddHttpClient<IGeocodingService, GeminiGeocodingService>();
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IActivityLogger, ActivityLogger>();
        services.AddScoped<IProposalPdfService, ProposalPdfService>();
        services.AddScoped<IWorkOrderPdfService, WorkOrderPdfService>();
        services.AddSingleton<IEmailService, SesEmailService>();

        return services;
    }
}
