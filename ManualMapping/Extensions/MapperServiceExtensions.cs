using ManualMapping.Abstractions;
using ManualMapping.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ManualMapping.Extensions;

public static class MapperServiceExtensions
{
    public static IServiceCollection AddMapper(
        this IServiceCollection services,
        Action<MapperConfiguration, IServiceProvider> configure)
    {
        services.AddSingleton<IMapper>(sp =>
        {
            var cfg = new MapperConfiguration();
            configure(cfg, sp);
            return cfg.Build();
        });
        return services;
    }
}
