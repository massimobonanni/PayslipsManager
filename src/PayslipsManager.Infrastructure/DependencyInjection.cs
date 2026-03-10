using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Application.Services;
using PayslipsManager.Infrastructure.Configuration;
using PayslipsManager.Infrastructure.Repositories;
using PayslipsManager.Infrastructure.Services;

namespace PayslipsManager.Infrastructure;

/// <summary>
/// Extension methods for configuring infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));

        var blobOptions = configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>()
            ?? new BlobStorageOptions();

        if (blobOptions.UseUserDelegatedAccess)
        {
            // Scoped: each request gets a BlobServiceClient authenticated as the signed-in user.
            // Azure RBAC on individual containers controls what each user can access.
            services.AddScoped(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
                var tokenAcquisition = sp.GetRequiredService<ITokenAcquisition>();
                var credential = new UserDelegatedTokenCredential(tokenAcquisition);
                return new BlobServiceClient(new Uri(options.AccountUrl), credential);
            });

            services.AddScoped<IPayslipStorageService, BlobPayslipRepository>();
        }
        else
        {
            // Singleton: shared client using managed identity or connection string.
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;

                if (options.UseManagedIdentity && !string.IsNullOrEmpty(options.AccountUrl))
                {
                    return new BlobServiceClient(new Uri(options.AccountUrl), new DefaultAzureCredential());
                }

                if (!string.IsNullOrEmpty(options.ConnectionString))
                {
                    return new BlobServiceClient(options.ConnectionString);
                }

                throw new InvalidOperationException(
                    "BlobStorage configuration is invalid. Provide either AccountUrl with UseManagedIdentity=true, or a ConnectionString for local development.");
            });

            services.AddSingleton<IPayslipStorageService, BlobPayslipRepository>();
        }

        // Register event processor
        services.AddScoped<IPayslipEventProcessor, PayslipEventProcessor>();

        // Register application services
        services.AddScoped<IPayslipQueryService, PayslipService>();
        services.AddScoped<IPayslipDownloadService>(sp => (PayslipService)sp.GetRequiredService<IPayslipQueryService>());

        return services;
    }
}
