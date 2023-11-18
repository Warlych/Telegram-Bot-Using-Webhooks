using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.Services;

public class ConfigureWebhook : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public ConfigureWebhook(
        IConfiguration configuration, 
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        
        var webhook = $"{_configuration["TelegramBot:WebhookUrl"]}/{_configuration["TelegramBot:BotRoute"]}";
        await client.SetWebhookAsync(url: webhook, 
            allowedUpdates: new [] { UpdateType.Message, UpdateType.ChannelPost, UpdateType.ChatMember, UpdateType.Unknown }, 
            cancellationToken: cancellationToken);
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
    }

}