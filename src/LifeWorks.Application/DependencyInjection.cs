using LifeWorks.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LifeWorks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IContractorService, ContractorService>();
        services.AddScoped<IHomeImprovementService, HomeImprovementService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        return services;
    }
}
