using Telegram.Bot.Types;

namespace TelegramBot.Application.Interfaces;

public interface IChannelFunction
{
    Task SetChannelAsync(Update update, CancellationToken cancellationToken);
    Task UnsetChannelAsync(Update update, CancellationToken cancellationToken);
    Task BanUserAsync(Message message, CancellationToken cancellationToken);
    Task UnbanUserAsync(Message message, CancellationToken cancellationToken);
}