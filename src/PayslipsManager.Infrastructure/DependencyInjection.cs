using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PayslipsManager.Application.Interfaces;
using PayslipsManager.Application.Services;
using PayslipsManager.Infrastructure.Configuration;
using PayslipsManager.Infrastructure.Repositories;

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

        // Register repositories
        services.AddSingleton<IPayslipRepository, BlobPayslipRepository>();

        // Register application services
        services.AddScoped<IPayslipService, PayslipService>();

        return services;
    }
}
