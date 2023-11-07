namespace TelegramBot.Infrastructure.Domain;

public class Topic
{
    public int TopicId { get; set; }
    public long GroupId { get; set; }
    public string Name { get; set; }
    public long OwnerId { get; set; }
}