namespace TelegramBot.Infrastructure.Domain;

public class GraphOfSubscribe
{
    public Guid Id { get; set; }
    public long ChannelId { get; set; }
    public DateTime EntryDate { get; set; }
}