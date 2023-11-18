using Telegram.Bot.Types.Enums;

namespace TelegramBot.Infrastructure.Domain;

public class Activity
{
    public Guid Id { get; set; }
    public long ActivityFromBotId { get; set; }
    public UpdateType UpdateType { get; set; }
    public ChatType ChatType { get; set; }
    public string? Text { get; set; }
    public DateTime Time { get; set; }
    public Topic? Topic { get; set; }
    public Consumer Consumer { get; set; }
}