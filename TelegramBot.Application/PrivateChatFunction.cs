using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

public class PrivateChatFunction : IPrivateChatFunction
{
    private readonly ITelegramBotClient _client;
    private readonly IDataContext _context;
    private readonly ILogger<IPrivateChatFunction> _logger;

    public PrivateChatFunction(ITelegramBotClient client, 
        IDataContext context, 
        ILogger<IPrivateChatFunction> logger)
    {
        _client = client;
        _context = context;
        _logger = logger;
    }

    public async Task BeginAsync(Message message, CancellationToken cancellationToken)
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
                    new KeyboardButton("/advt"),
                    new KeyboardButton("/news"),
                }
            }),
            cancellationToken: cancellationToken
        );

    }

    public async Task HelpAsync(Message message, CancellationToken cancellationToken)
    {
        var response = "/ask - command to send a question to the administration, \n" +
                       "/advt - command to send an advertising proposal, \n" +
                       "/news - command to send a news.";

        await _client.SendTextMessageAsync(
            chatId: message.Chat,
            text: response,
            cancellationToken: cancellationToken);
    }

    public async Task AskAsync(Message message, CancellationToken cancellationToken)
    {
        var condition = await IsGroupAcceptMessageAsync(message, cancellationToken);
        if (!condition)
            return;

        var response =
            "What question do you want to ask? (Write a reply to this message. You can attach a document, you should do the same with photos)";
        await SendingReplyMessageByContextAsync(message, response, cancellationToken);
    }

    public async Task AdvtAsync(Message message, CancellationToken cancellationToken)
    {
        var condition = await IsGroupAcceptMessageAsync(message, cancellationToken);
        if (!condition)
            return;

        var response =
            "What would you like to suggest? (Write a reply to this message. You can attach a document, you should do the same with photos)";
        await SendingReplyMessageByContextAsync(message, response, cancellationToken);
    }

    public async Task NewsAsync(Message message, CancellationToken cancellationToken)
    {
        var condition = await IsGroupAcceptMessageAsync(message, cancellationToken);
        if (!condition)
            return;

        var response =
            "What news do you want to offer? (Write a reply to this message. You can attach a document, you should do the same with photos)";
        await SendingReplyMessageByContextAsync(message, response, cancellationToken);
    }

    public async Task ReplyToBotMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.ReplyToMessage.From.Id != _client.BotId)
            return;

        if (message.Text.StartsWith('/'))
        {
            await _client.SendTextMessageAsync(chatId: message.Chat,
                text: @"Reply to messages cannot be a message starting with ""/""",
                cancellationToken: cancellationToken);
            return;
        }

        var handler = message switch
        {
            {
                    ReplyToMessage:
                    {
                        Text:
                        "What question do you want to ask? (Write a reply to this message. You can attach a document, you should do the same with photos)"
                    }
                }
                => SendingQuestionAsync(message, cancellationToken),
            {
                    ReplyToMessage:
                    {
                        Text:
                        "What would you like to suggest? (Write a reply to this message. You can attach a document, you should do the same with photos)"
                    }
                }
                => SendingAdvtAsync(message, cancellationToken),
            {
                    ReplyToMessage:
                    {
                        Text:
                        "What news do you want to offer? (Write a reply to this message. You can attach a document, you should do the same with photos)"
                    }
                }
                => SendingNewsAsync(message, cancellationToken),
            _ => UnknownReplyToBotMessageAsync(message, cancellationToken)
        };

        await handler;
    }

    private async Task SendingQuestionAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();
        var ownerId = message.Chat.Id;

        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.GroupId == groupId
                                      && t.OwnerId == ownerId
                                      && t.TopicType == TopicType.Ask);

        if (topic == null)
            topic = await CreatingTopicByContextAsync(groupId, TopicType.Ask, message, cancellationToken);

        await _client.SendTextMessageAsync(chatId: ownerId,
            text: $"U asked: {message.Text}. We'll answered soon.",
            cancellationToken: cancellationToken);

        await _client.SendTextMessageAsync(chatId: groupId,
            text: $"@{message.Chat.Username} asked: {message.Text}",
            messageThreadId: topic.TopicId);

        if (message.Document != null)
        {
            await Helper.SendingDocumentAsync(_client, null, topic, message, cancellationToken);
            _logger.LogInformation("Document {@document} was send", message.Document);
        }
    }

    private async Task SendingAdvtAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();
        var ownerId = message.Chat.Id;

        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.GroupId == groupId
                                      && t.OwnerId == ownerId
                                      && t.TopicType == TopicType.Advt);

        if (topic == null)
            topic = await CreatingTopicByContextAsync(groupId, TopicType.Advt, message, cancellationToken);
        
        await _client.SendTextMessageAsync(chatId: ownerId,
            text: $"U suggest: {message.Text}. We'll answered soon.",
            cancellationToken: cancellationToken);
        
        await _client.SendTextMessageAsync(chatId: groupId,
            text: $"@{message.Chat.Username} suggested: {message.Text}",
            messageThreadId: topic.TopicId);
        
        if (message.Document != null)
        {
            await Helper.SendingDocumentAsync(_client, null, topic, message, cancellationToken);
            _logger.LogInformation("Document {@document} was send", message.Document);
        }
    }

    private async Task SendingNewsAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();
        var ownerId = message.Chat.Id;

        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.GroupId == groupId
                                      && t.OwnerId == ownerId
                                      && t.TopicType == TopicType.News);

        if (topic == null)
            topic = await CreatingTopicByContextAsync(groupId, TopicType.News, message, cancellationToken);
        
        await _client.SendTextMessageAsync(chatId: ownerId,
            text: $"U offer: {message.Text}. We'll answered soon.",
            cancellationToken: cancellationToken);

        await _client.SendTextMessageAsync(chatId: groupId,
            text: $"@{message.Chat.Username} offered: {message.Text}",
            messageThreadId: topic.TopicId);

        if (message.Document != null)
        {
            await Helper.SendingDocumentAsync(_client, null, topic, message, cancellationToken);
            _logger.LogInformation("Document {@document} was send", message.Document);
        }
    }

    private async Task<bool> IsGroupAcceptMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();

        if (groupId.Equals(0))
        {
            await _client.SendTextMessageAsync(chatId: message.Chat,
                text: "It's not possible to ask a question at this time. Try later.",
                cancellationToken: cancellationToken);

            return false;
        }

        return true;
    }

    private async Task<Topic> CreatingTopicByContextAsync(long groupId, TopicType topicType, Message message,
        CancellationToken cancellationToken)
    {
        var name = topicType switch
        {
            TopicType.Ask => $"{message.Chat.FirstName} - Ask",
            TopicType.Advt => $"{message.Chat.FirstName} - Advt",
            TopicType.News => $"{message.Chat.FirstName} - News"
        };

        var topicForum = await _client.CreateForumTopicAsync(chatId: groupId,
            name: name,
            cancellationToken: cancellationToken);

        var topic = new Topic()
        {
            TopicId = topicForum.MessageThreadId,
            GroupId = groupId,
            Name = name,
            OwnerId = message.Chat.Id,
            TopicType = topicType
        };

        await _context.Topics.AddAsync(topic, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new {@topic}", topic);
        return topic;
    }

    private async Task SendingReplyMessageByContextAsync(Message message, string response,
        CancellationToken cancellationToken)
    {
        var replyMarkup = new ForceReplyMarkup() { Selective = true };
        await _client.SendTextMessageAsync(
            chatId: message.Chat,
            text: response,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }
    
    private async Task UnknownReplyToBotMessageAsync(Message message, CancellationToken cancellationToken)
    {
        await _client.SendTextMessageAsync(chatId: message.Chat,
            text: "I didn't understand u",
            cancellationToken: cancellationToken);
    }
}