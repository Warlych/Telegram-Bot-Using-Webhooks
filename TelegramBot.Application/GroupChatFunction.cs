using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Application.Common;
using TelegramBot.Application.Interfaces;
using TelegramBot.Infrastructure.Domain;
using TelegramBot.Infrastructure.Domain.Enums;
using TelegramBot.Infrastructure.Interfaces;
using File = System.IO.File;

namespace TelegramBot.Application;

public class GroupChatFunction : IGroupChatFunction
{
    private readonly ITelegramBotClient _client;
    private readonly IDataContext _context;
    private readonly ILogger<IGroupChatFunction> _logger;

    public GroupChatFunction(ITelegramBotClient client, IDataContext context, ILogger<IGroupChatFunction> logger)
    {
        _client = client;
        _context = context;
        _logger = logger;
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
                       "/send - command to send a response to the user (use in topics), \n" +
                       "/close_topic - command to close a topic, use inside a topic, \n" +
                       "/topic_statistics - command to get statistics on topics, \n" +
                       @"/topic_statistics_date ""dd-MM-yyyy"" - command to get statistics on topics by date, \n" +
                       "/ban username:reason - command to ban a user in a channel, \n" +
                       "/unban username - command to unban a user in a channel";

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
        
        _logger.LogInformation("Group {groupId} was set", groupId);
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
        
        _logger.LogInformation("Group {groupId} was unset", groupId);
    }

    public async Task CloseTopicAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();
        
        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.GroupId == groupId
                                      && t.TopicId == message.MessageThreadId
                                      && t.ClosingDate == null);
        
        await _client.DeleteForumTopicAsync(chatId: topic.GroupId, 
            messageThreadId: topic.TopicId, 
            cancellationToken: cancellationToken);
        
        _logger.LogInformation("Topic: {@topic} closed", topic);
        topic.ClosingDate = DateTime.Now.ToUniversalTime();
        await _context.SaveChangesAsync(cancellationToken);
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
            {
                ReplyToMessage:
                {
                    Text:
                    "To send a response, reply to this message. (You can attach a document, you should do the same with photos)"
                }
            } => SendingAnswerAsync(message,
                cancellationToken),
            {
                ReplyToMessage:
                {
                    Text:
                    @"The user was not found, you can try to update the database, but it may take time. Reply ""Yes"" to this message to begin the process."
                }
            } => UpdatingConsumerDataAsync(message, cancellationToken),
            _ => UnknownReplyToBotMessageAsync(message, cancellationToken)
        };

        await handler;
    }

    private async Task SendingAnswerAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();

        var topic = await _context.Topics
            .Include(t => t.TopicActivies)
            .FirstOrDefaultAsync(t => t.GroupId == groupId
                                      && t.TopicId == message.MessageThreadId
                                      && t.ClosingDate == null);

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
                $"Administrator {message.From.FirstName} answered: {message.Text} to ur ask: {topic.TopicActivies.ElementAt(topic.TopicActivies.Count - 2)}",
            { TopicType: TopicType.Advt } =>
                $"Administrator {message.From.FirstName} answered: {message.Text} to ur advt offer: {topic.TopicActivies.ElementAt(topic.TopicActivies.Count - 2)}",
            { TopicType: TopicType.News } =>
                $"Administrator {message.From.FirstName} answered: {message.Text} to ur news offer: {topic.TopicActivies.ElementAt(topic.TopicActivies.Count - 2)}"
        };

        await _client.SendTextMessageAsync(chatId: topic.OwnerId,
            text: response,
            cancellationToken: cancellationToken);

        if (message.Document != null)
        {
            await Helper.SendingDocumentAsync(_client, topic.OwnerId, null, message, cancellationToken);
            _logger.LogInformation("Document {@document} was send", message.Document);
        }
    }

    private async Task UpdatingConsumerDataAsync(Message message, CancellationToken cancellationToken)
    {
        await _client.SendTextMessageAsync(chatId: message.Chat,
            text: "The update has started, Expect the end.",
            cancellationToken: cancellationToken);
        
        var consumers = _context.Consumers.ToArray();

        foreach (var consumer in consumers)
        {
            var newInfo = await _client.GetChatAsync(consumer.ConsumerId);

            if (consumer.Name != newInfo.Username)
            {
                consumer.Name = newInfo.Username;
                await _context.SaveChangesAsync(cancellationToken);
            }
            
            Task.Delay(15000);   
        }
        
        await _client.SendTextMessageAsync(chatId: message.Chat,
            text: "The update has finished. Try banning the user again.",
            cancellationToken: cancellationToken);
    }
    
    private async Task UnknownReplyToBotMessageAsync(Message message, CancellationToken cancellationToken)
    {
        await _client.SendTextMessageAsync(chatId: message.Chat,
            text: "I didn't understand u.",
            cancellationToken: cancellationToken);
    }
}