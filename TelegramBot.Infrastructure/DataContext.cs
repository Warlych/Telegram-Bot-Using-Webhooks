using Microsoft.EntityFrameworkCore;
using TelegramBot.Infrastructure.Domain;
using TelegramBot.Infrastructure.Interfaces;

namespace TelegramBot.Infrastructure;

public class DataContext : DbContext, IDataContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Activity>().HasKey(a => a.Id);
        builder.Entity<Topic>().HasKey(t => t.TopicId);

        builder.Entity<Topic>().HasMany(t => t.TopicActivies)
            .WithOne(a => a.Topic)
            .OnDelete(DeleteBehavior.Cascade);
        
        base.OnModelCreating(builder);
    }
    
    public DbSet<Activity> Activities { get; set; }
    public DbSet<Topic> Topics { get; set; }
}