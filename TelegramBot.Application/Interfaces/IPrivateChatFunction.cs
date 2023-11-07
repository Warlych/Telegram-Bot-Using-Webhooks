using Telegram.Bot.Types;

namespace TelegramBot.Application.Interfaces;

public interface IPrivateChatFunction
{
    Task Begin(Message message, CancellationToken cancellationToken);
    Task Help(Message message, CancellationToken cancellationToken);
    Task Ask(Message message, CancellationToken cancellationToken);
    Task ReplyToBotMessage(Message message, CancellationToken cancellationToken);
}