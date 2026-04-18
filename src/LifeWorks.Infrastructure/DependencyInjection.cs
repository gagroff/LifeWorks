using LifeWorks.Application.Repositories;
using LifeWorks.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LifeWorks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IContractorRepository, ContractorRepository>();
        services.AddScoped<IHomeImprovementRepository, HomeImprovementRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        return services;
    }
}
