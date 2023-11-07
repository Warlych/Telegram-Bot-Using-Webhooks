namespace TelegramBot.Infrastructure;

public static class DbInitializer
{
    public static void Initialize(DataContext context)
    {
        context.Database.EnsureCreated();
    }
}