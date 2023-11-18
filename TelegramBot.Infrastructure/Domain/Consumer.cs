using Telegram.Bot.Types.Enums;

namespace TelegramBot.Infrastructure.Domain;

public class Consumer
{
    public long ConsumerId { get; set; }
    public string Name { get; set; }
    public bool? IsBot { get; set; }
    public DateTime EntryDate { get; set; }
    public ICollection<BanInfo>? Bans { get; set; }
}