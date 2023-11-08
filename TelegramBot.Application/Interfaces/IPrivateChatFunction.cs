using Telegram.Bot.Types;

namespace TelegramBot.Application.Interfaces;

public interface IPrivateChatFunction
{
    Task BeginAsync(Message message, CancellationToken cancellationToken);
    Task HelpAsync(Message message, CancellationToken cancellationToken);
    Task AskAsync(Message message, CancellationToken cancellationToken);
    Task ReplyToBotMessageAsync(Message message, CancellationToken cancellationToken);
}