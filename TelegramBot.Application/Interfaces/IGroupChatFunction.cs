using Telegram.Bot.Types;
using TelegramBot.Infrastructure.Interfaces;

namespace TelegramBot.Application.Interfaces;

public interface IGroupChatFunction
{ 
    Task Begin(Message message, CancellationToken cancellationToken);
    Task Help(Message message, CancellationToken cancellationToken);
    Task SetGroup(Message message, CancellationToken cancellationToken);
    Task UnsetGroup(Message message, CancellationToken cancellationToken);
}