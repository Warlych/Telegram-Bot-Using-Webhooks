using Telegram.Bot;
using TelegramBot.Application.DependencyInjection;
using TelegramBot.Controllers;
using TelegramBot.Infrastructure.DependencyInjection;
using TelegramBot.Services;

var builder = WebApplication.CreateBuilder(args);

var token = builder.Configuration["TelegramBotToken"];

builder.Services.AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>((httpClient) =>
    {
        TelegramBotClientOptions options = new(token);
        return new TelegramBotClient(options, httpClient);
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<UpdateHandlers>();

builder.Services.AddHostedService<ConfigureWebhook>();

builder.Services.AddControllers()
    .AddNewtonsoftJson();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
        policy.AllowAnyOrigin();
    });
});


var app = builder.Build();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(name: "webhook_route", 
        pattern: builder.Configuration["TelegramBot:BotRoute"], 
        defaults: new { controller = "Bot", action = "Update" });
});

app.MapControllers();
app.Run();