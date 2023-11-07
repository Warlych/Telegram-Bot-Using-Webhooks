using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramBot.Infrastructure.Interfaces;

namespace TelegramBot.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DataContext>(options =>
            options.UseNpgsql(configuration["DbConnection"]));
        services.AddScoped<IDataContext, DataContext>();
        
        DbInitializer.Initialize(services.BuildServiceProvider().GetService<DataContext>());

        return services;
    }
}