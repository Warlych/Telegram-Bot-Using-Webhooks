using Microsoft.Extensions.DependencyInjection;
using TelegramBot.Application.Interfaces;

namespace TelegramBot.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPrivateChatFunction, PrivateChatFunction>();
        services.AddScoped<IGroupChatFunction, GroupChatFunction>();
        services.AddScoped<IStatisticsFunction, StatisticsFunction>();
        
        return services;
    }
}