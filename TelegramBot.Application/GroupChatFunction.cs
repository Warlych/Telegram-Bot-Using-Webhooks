using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Application.Common;
using TelegramBot.Application.Interfaces;
using TelegramBot.Infrastructure.Domain.Enums;
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

    public async Task BeginAsync(Message message, CancellationToken cancellationToken)
    {
        await _client.SendTextMessageAsync(
            chatId: message.Chat,
            text:
            @"Where would u like to start? Use ""/set_group"" to set the main group and ""/help"" to see commands",
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

    public async Task HelpAsync(Message message, CancellationToken cancellationToken)
    {
        var response = "/set_group - command to set the main group, \n" +
                       "/unset_group - command to unset the main group, \n" +
                       "/send - command to send a response to the user (use in topics).";

        await _client.SendTextMessageAsync(
            chatId: message.Chat,
            text: response,
            cancellationToken: cancellationToken);
    }

    public async Task SetGroupAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = message.Chat.Id;

        var filePath = Environment.CurrentDirectory + "/Properties/userSettings.json";

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        data["Group"] = groupId.ToString();

        json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
        await _client.SendTextMessageAsync(message.Chat, "Group was set.");
    }

    public async Task UnsetGroupAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = message.Chat.Id;

        var filePath = Environment.CurrentDirectory + "/Properties/userSettings.json";

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        if (data["Group"] == groupId.ToString())
            data["Group"] = "0";

        json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);

        await _client.SendTextMessageAsync(message.Chat, "Group was unset.");
    }

    public async Task SendingResponseAsync(Message message, CancellationToken cancellationToken)
    {
        var replyMarkup = new ForceReplyMarkup() { Selective = true };
        await _client.SendTextMessageAsync(chatId: message.Chat,
            text: "To send a response, reply to this message. (You can attach a document, you should do the same with photos)",
            messageThreadId: message.MessageThreadId,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    public async Task ReplyToBotMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.ReplyToMessage.From.Id != _client.BotId)
            return;

        var handler = message switch
        {
            { ReplyToMessage: { Text: "To send a response, reply to this message. (You can attach a document, you should do the same with photos)" } } => SendingAnswerAsync(message,
                cancellationToken),
            _ => UnknownReplyToBotMessageAsync(message, cancellationToken)
        };

        await handler;
    }

    private async Task SendingAnswerAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();

        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.GroupId == groupId
                                      && t.TopicId == message.MessageThreadId);

        if (topic == null)
        {
            await _client.SendTextMessageAsync(chatId: groupId,
                text: "The chat may have been lost. (Contact the person directly)",
                messageThreadId: message.MessageThreadId,
                cancellationToken: cancellationToken);

            return;
        }

        var response = topic switch
        {
            { TopicType: TopicType.Ask } =>
                $"Administrator {message.From.FirstName} answered: {message.Text} to ur ask",
            { TopicType: TopicType.Advt } =>
                $"Administrator {message.From.FirstName} answered: {message.Text} to ur advt offer",
            { TopicType: TopicType.News } =>
                $"Administrator {message.From.FirstName} answered: {message.Text} to ur news offer"
        };

        await _client.SendTextMessageAsync(chatId: topic.OwnerId,
            text: response,
            cancellationToken: cancellationToken);

        if (message.Document != null)
            await Helper.SendingDocumentAsync(_client, topic.OwnerId, null, message, cancellationToken);

    }

    private async Task UnknownReplyToBotMessageAsync(Message message, CancellationToken cancellationToken)
    {
        await _client.SendTextMessageAsync(chatId: message.Chat,
            text: "I didn't understand u",
            cancellationToken: cancellationToken);
    }
}