using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Application.Interfaces;
using TelegramBot.Infrastructure.Interfaces;
using File = System.IO.File;

namespace TelegramBot.Application;

public class PrivateChatFunction : IPrivateChatFunction
{
    private readonly ITelegramBotClient _client;
    private readonly IDataContext _context;
    
    public PrivateChatFunction(ITelegramBotClient client, IDataContext context)
    {
        _client = client;
        _context = context;
    }
    
    public async Task Begin(Message message, CancellationToken cancellationToken)
    {
        await _client.SendTextMessageAsync(
            chatId: message.Chat,
            text: "Where would u like to start?",
            replyMarkup: new ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    new KeyboardButton("/help")
                },
                new[]
                {
                    new KeyboardButton("/ask"),
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
            text: ")",
            cancellationToken: cancellationToken);
    }

    public async Task Ask(Message message, CancellationToken cancellationToken)
    {
        var groupId = await GetGroupId();
        
        if (groupId.Equals(0))
        {
            await _client.SendTextMessageAsync(chatId: message.Chat,
                text: "It's not possible to ask a question at this time. Try later.",
                cancellationToken: cancellationToken);
            
            return;
        }
        
        var replyMarkup = new ForceReplyMarkup() { Selective = true };
        await _client.SendTextMessageAsync(
            chatId: message.Chat,
            text: "What question do you want to ask? (Write a reply to this message)",
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    public Task ReplyToBotMessage(Message message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    
    private async Task<long> GetGroupId()
    {
        var filePath = Environment.CurrentDirectory + "/Properties/userSettings.json";

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        var groupId = Convert.ToInt64(data["Group"]);
        return groupId;
    }

}