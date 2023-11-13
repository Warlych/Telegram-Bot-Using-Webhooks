using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Application.Common;
using TelegramBot.Application.Interfaces;
using TelegramBot.Infrastructure.Interfaces;
using File = System.IO.File;

namespace TelegramBot.Application;

public class ChannelFunction : IChannelFunction
{
    private readonly ITelegramBotClient _client;
    private readonly IDataContext _context;
    private readonly ILogger<IChannelFunction> _logger;

    public ChannelFunction(ITelegramBotClient client, 
        IDataContext context, 
        ILogger<IChannelFunction> logger)
    {
        _client = client;
        _context = context;
        _logger = logger;
    }
    
    public async Task SetChannelAsync(Update update, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();
        var channelId = update.ChannelPost.Chat.Id;
        
        var filePath = Environment.CurrentDirectory + "/Properties/userSettings.json";

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        data["Channel"] = channelId.ToString();

        json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
        await _client.SendTextMessageAsync(groupId, "Channel was set.");
        
        _logger.LogInformation("Channel {channelId} was set", channelId);
    }

    public async Task UnsetChannelAsync(Update update, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();
        var channelId = update.ChannelPost.Chat.Id;
        
        var filePath = Environment.CurrentDirectory + "/Properties/userSettings.json";

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        if (data["Channel"] == channelId.ToString())
            data["Channel"] = "0";

        json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
        await _client.SendTextMessageAsync(groupId, "Channel was unset.");
        
        _logger.LogInformation("Channel {channelId} was unset", channelId);
    }
}