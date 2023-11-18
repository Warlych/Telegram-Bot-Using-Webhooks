namespace TelegramBot.Infrastructure.Domain;

public class BanInfo
{
    public Guid BanInfoId { get; set; }
    public Consumer Consumer { get; set; }
    public string Reason { get; set; }
    public long ChatId { get; set; }
}