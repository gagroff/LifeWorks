using LifeWorks.Application.Repositories;
using LifeWorks.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LifeWorks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IContractorRepository, ContractorRepository>();
        services.AddScoped<IHomeImprovementRepository, HomeImprovementRepository>();
        services.AddScoped<IMaintenanceTaskRepository, MaintenanceTaskRepository>();
        services.AddScoped<IMaintenanceLogRepository, MaintenanceLogRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        return services;
    }
}
