using Microsoft.EntityFrameworkCore;
using TelegramBot.Infrastructure.Domain;

namespace TelegramBot.Infrastructure.Interfaces;

public interface IDataContext
{
    DbSet<Activity> Activities { get; set; }
    DbSet<Topic> Topics { get; set; }
    DbSet<Consumer> Consumers { get; set; }
    DbSet<BanInfo> Bans { get; set; }
    DbSet<GraphOfSubscribe> Subscribes { get; set; }
    int SaveChanges();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}