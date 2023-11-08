using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Application.Common;
using TelegramBot.Application.Interfaces;
using TelegramBot.Infrastructure.Domain;
using TelegramBot.Infrastructure.Interfaces;

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
                }
            }),
            cancellationToken: cancellationToken
        );

    }

    public async Task HelpAsync(Message message, CancellationToken cancellationToken)
    {
        await _client.SendTextMessageAsync(
            chatId: message.Chat,
            text: "/ask - command to send a question to the administration.",
            cancellationToken: cancellationToken);
    }

    public async Task AskAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();

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

    public async Task ReplyToBotMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var handler = message switch
        {
            { ReplyToMessage: { Text: "What question do you want to ask? (Write a reply to this message)" } }
                => SendingQuestionAsync(message, cancellationToken),
        };

        await handler;
    }

    private async Task SendingQuestionAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();
        var ownerId = message.Chat.Id;

        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.GroupId == groupId && t.OwnerId == ownerId);

        if (topic == null)
        {
            var name = $"{message.Chat.FirstName} - Ask";

            var topicForum = await _client.CreateForumTopicAsync(chatId: groupId,
                name: name,
                cancellationToken: cancellationToken);

            topic = new Topic()
            {
                TopicId = topicForum.MessageThreadId,
                GroupId = groupId,
                Name = name,
                OwnerId = ownerId
            };

            await _context.Topics.AddAsync(topic, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _client.SendTextMessageAsync(chatId: ownerId,
            text: $"U asked: {message.Text}. U'll answered soon",
            cancellationToken: cancellationToken);

        await _client.SendTextMessageAsync(chatId: groupId,
            text: $"@{message.Chat.Username} asked: {message.Text}",
            messageThreadId: topic.TopicId);
    }
}