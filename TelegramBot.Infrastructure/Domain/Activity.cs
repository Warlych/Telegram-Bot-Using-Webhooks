using Telegram.Bot.Types.Enums;

namespace TelegramBot.Infrastructure.Domain;

public class Activity
{
    public Guid Id { get; set; }
    public long MessageId { get; set; }
    public bool IsBot { get; set; }
    public long ChatId { get; set; }
    public ChatType ChatType { get; set; }
    public string? Message { get; set; }
    public DateTime Time { get; set; }
    public Topic? Topic { get; set; }
}