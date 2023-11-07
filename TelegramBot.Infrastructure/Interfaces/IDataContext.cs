using Microsoft.EntityFrameworkCore;
using TelegramBot.Infrastructure.Domain;

namespace TelegramBot.Infrastructure.Interfaces;

public interface IDataContext
{
    DbSet<Activity> Activities { get; set; }
    DbSet<Topic> Topics { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}