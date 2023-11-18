using Telegram.Bot.Types;

namespace TelegramBot.Application.Interfaces;

public interface IStatisticsFunction
{
    Task TopicStatisticAsync(Message message, CancellationToken cancellationToken);
    Task TopicStatisticByDateAsync(Message message, CancellationToken cancellationToken);
    Task ChannelSubscribeStatisticAsync(Message message, CancellationToken cancellationToken);
}