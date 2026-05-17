using LifeWorks.Application.Repositories;
using LifeWorks.Application.Storage;
using LifeWorks.Infrastructure.Repositories;
using LifeWorks.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LifeWorks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IFileStorage, LocalFileStorage>();
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
