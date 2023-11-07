using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Application.Interfaces;
using TelegramBot.Infrastructure.Interfaces;
using File = System.IO.File;

namespace TelegramBot.Application;

public class GroupChatFunction : IGroupChatFunction
{
    private readonly ITelegramBotClient _client;
    private readonly IDataContext _context;

    public GroupChatFunction(ITelegramBotClient client, IDataContext context)
    {
        _client = client;
        _context = context;
    }
    
    public async Task Begin(Message message, CancellationToken cancellationToken)
    {
        await _client.SendTextMessageAsync(
            chatId: message.Chat,
            text: @"Where would u like to start? Use ""/set_group"" to set the main group and ""/help"" to see comands",
            replyMarkup: new ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    new KeyboardButton("/set_group"),
                    new KeyboardButton("/unset_group")
                }
            }),
            cancellationToken: cancellationToken
        );
        
    }

    public async Task Help(Message message, CancellationToken cancellationToken)
    {
        // will be function description
        await _client.SendTextMessageAsync(
            chatId: message.Chat,
            text: "",
            cancellationToken: cancellationToken);
    }

    public async Task SetGroup(Message message, CancellationToken cancellationToken)
    {
        var groupId = message.Chat.Id;

        var filePath = Environment.CurrentDirectory + "/Properties/userSettings.json";

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        data["Group"] = groupId.ToString();

        json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
        await _client.SendTextMessageAsync(message.Chat, "Group was set");
    }

    public async Task UnsetGroup(Message message, CancellationToken cancellationToken)
    {
        var groupId = message.Chat.Id;

        var filePath = Environment.CurrentDirectory + "/Properties/userSettings.json";


        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        if (data["Group"] == groupId.ToString())
            data["Group"] = String.Empty;

        json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
        
        await _client.SendTextMessageAsync(message.Chat, "Group was unset");
    }

}