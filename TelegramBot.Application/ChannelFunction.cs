using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Application.Common;
using TelegramBot.Application.Interfaces;
using TelegramBot.Infrastructure.Domain;
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
    
    public async Task BanUserAsync(Message message, CancellationToken cancellationToken)
    {
        var channelId = await Helper.GetChannelIdAsync();

        var msg = message.Text.Split(' ');

        if (msg.Length <= 1)
        {
            await _client.SendTextMessageAsync(chatId: message.Chat,
                text: @"In order to ban a user you must send: ""/ban username:ban_reason""",
                cancellationToken: cancellationToken);

            return;
        }
        
        var form = msg[1].Split(':');

        var consumer = await _context.Consumers
            .FirstOrDefaultAsync(c => c.Name == form[0]);

        if (consumer == null)
        {
            await _client.SendTextMessageAsync(chatId: message.Chat,
                text:
                @"The user was not found, you can try to update the database, but it may take time. Reply ""Yes"" to this message to begin the process.",
                cancellationToken: cancellationToken);

            return;
        }
        
        var member = await _client.GetChatMemberAsync(chatId: channelId,
            userId: consumer.ConsumerId,
            cancellationToken: cancellationToken);

        if (member == null)
        {
            await _client.SendTextMessageAsync(chatId: message.Chat,
                text: "This user is not subscribed to the channel.",
                cancellationToken: cancellationToken);

            return;
        }
        
        var banInfo = new BanInfo()
        {
            BanInfoId = Guid.NewGuid(),
            Consumer = consumer,
            Reason = form[1],
            ChatId = channelId
        };

        await _client.BanChatMemberAsync(chatId: channelId, userId: consumer.ConsumerId,
            cancellationToken: cancellationToken);
        
        await _context.Bans.AddAsync(banInfo, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("User {@user} was banned", consumer);

        await _client.SendTextMessageAsync(chatId: message.Chat,
            text: $"User {consumer.Name} was banned.",
            cancellationToken: cancellationToken);
    }

    public async Task UnbanUserAsync(Message message, CancellationToken cancellationToken)
    {
        var channelId = await Helper.GetChannelIdAsync();

        var msg = message.Text.Split(' ');

        if (msg.Length <= 1)
        {
            await _client.SendTextMessageAsync(chatId: message.Chat,
                text: @"In order to unban a user you must send: ""/unban username""",
                cancellationToken: cancellationToken);

            return;
        }
        
        var consumer = await _context.Consumers
            .Include(c => c.Bans)
            .FirstOrDefaultAsync(c => c.Name == msg[1]);

        if (consumer == null)
        {
            await _client.SendTextMessageAsync(chatId: message.Chat,
                text:
                @"The user was not found, you can try to update the database, but it may take time. Reply ""Yes"" to this message to begin the process.",
                cancellationToken: cancellationToken);

            return;
        }

        BanInfo banForChannel = null;
        foreach (var ban in consumer.Bans)
        {
            if (ban.ChatId == channelId)
            {
                banForChannel = ban;
                break;
            }
        }

        if (banForChannel == null)
        {
            await _client.SendTextMessageAsync(chatId: message.Chat,
                text: "The user is not banned from your channel",
                cancellationToken: cancellationToken);

            return;
        }

        await _client.UnbanChatMemberAsync(chatId: channelId,
            userId: consumer.ConsumerId,
            cancellationToken: cancellationToken);

        _context.Bans.Remove(banForChannel);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("User {@user} was unbanned", consumer);
        
        await _client.SendTextMessageAsync(chatId: message.Chat,
            text: $"User {consumer.Name} was unbanned.",
            cancellationToken: cancellationToken);
    }
    
    private async Task UnknownReplyToBotMessageAsync(Message message, CancellationToken cancellationToken)
    {
        await _client.SendTextMessageAsync(chatId: message.Chat,
            text: "I didn't understand u.",
            cancellationToken: cancellationToken);
    }
}