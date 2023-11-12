using Telegram.Bot.Types;
using TelegramBot.Infrastructure.Interfaces;

namespace TelegramBot.Application.Interfaces;

public interface IGroupChatFunction
{ 
    Task BeginAsync(Message message, CancellationToken cancellationToken);
    Task HelpAsync(Message message, CancellationToken cancellationToken);
    Task SetGroupAsync(Message message, CancellationToken cancellationToken);
    Task UnsetGroupAsync(Message message, CancellationToken cancellationToken);
    Task CloseTopicAsync(Message message, CancellationToken cancellationToken);
    Task SendingResponseAsync(Message message, CancellationToken cancellationToken);
    Task ReplyToBotMessageAsync(Message message, CancellationToken cancellationToken);
}