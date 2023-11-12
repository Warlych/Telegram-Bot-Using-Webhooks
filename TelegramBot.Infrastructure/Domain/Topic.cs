using TelegramBot.Infrastructure.Domain.Enums;

namespace TelegramBot.Infrastructure.Domain;

public class Topic
{
    public int TopicId { get; set; }
    public long GroupId { get; set; }
    public string Name { get; set; }
    public long OwnerId { get; set; }
    public TopicType TopicType { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? ClosingDate { get; set; }
    public ICollection<Activity> TopicActivies { get; set; }
}