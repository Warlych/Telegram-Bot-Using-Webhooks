using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Application.Interfaces;
using TelegramBot.Infrastructure.Domain;
using TelegramBot.Infrastructure.Interfaces;

namespace TelegramBot.Services;

public class UpdateHandlers
{
    private readonly ITelegramBotClient _client;
    private readonly IDataContext _context;
    private readonly ILogger<UpdateHandlers> _logger;

    private readonly IPrivateChatFunction _privateChatFunction;
    private readonly IGroupChatFunction _groupChatFunction;
    private readonly IStatisticsFunction _statisticsFunction;
    
    public UpdateHandlers(ITelegramBotClient client, 
        IDataContext context, 
        IPrivateChatFunction privateChatFunction, 
        IGroupChatFunction groupChatFunction,
        IStatisticsFunction statisticsFunction,
        ILogger<UpdateHandlers> logger)
    {
        _client = client;
        _context = context;
        _logger = logger;
        _privateChatFunction = privateChatFunction;
        _groupChatFunction = groupChatFunction;
        _statisticsFunction = statisticsFunction;
    }
    
    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _                                       => exception.ToString()
        };
        
        _logger.LogError(errorMessage);
        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Message == null)
            return;
        
        Topic topic = null;
        if (update.Message.MessageThreadId != null)
            topic = await _context.Topics
                .FirstOrDefaultAsync(t => t.TopicId == update.Message.MessageThreadId);
        
        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            MessageId = update.Message.MessageId,
            IsBot = update.Message.From.IsBot,
            ChatId = update.Message.Chat.Id,
            ChatType = update.Message.Chat.Type,
            Message = update.Message.Text,
            Time = DateTime.Now.ToUniversalTime(),
            Topic = topic 
        };

        await _context.Activities.AddAsync(activity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
      
        _logger.LogInformation("New update: {@update}", update);
        _logger.LogInformation("Message: {message}", activity.Message);
        
        var handler = update switch
        {
            { Message: { Chat.Type: ChatType.Private } message } => BotOnPrivateMessageReceiving(message,
                cancellationToken),
            { Message: { Chat.Type: ChatType.Group or ChatType.Supergroup } message} => BotOnGroupMessageReceiving(
                message, cancellationToken),
            _ => UnknownUpdate(update, cancellationToken)
        };

        await handler;
    }
    
    private async Task BotOnPrivateMessageReceiving(Message message, CancellationToken cancellationToken)
    {
        var func = message switch
        {
            { Text: "/start" } => _privateChatFunction.BeginAsync(message, cancellationToken),
            { Text: "/help" } => _privateChatFunction.HelpAsync(message, cancellationToken),
            { Text: "/ask" } => _privateChatFunction.AskAsync(message, cancellationToken),
            { Text: "/advt" } => _privateChatFunction.AdvtAsync(message, cancellationToken),
            { Text: "/news" } => _privateChatFunction.NewsAsync(message, cancellationToken),
            { ReplyToMessage.Text: not null } => _privateChatFunction.ReplyToBotMessageAsync(message, cancellationToken),
            _ => _client.SendTextMessageAsync(message.Chat, "I didn't understand u")
        };
        
        _logger.LogInformation("Command {@command} executed", func);
        await func;
    }

    private async Task BotOnGroupMessageReceiving(Message message, CancellationToken cancellationToken)
    {
        var func = message switch
        {
            { Text: "/start" } => _groupChatFunction.BeginAsync(message, cancellationToken),
            { Text: "/help" } => _groupChatFunction.HelpAsync(message, cancellationToken),
            { Text: "/set_group" } => _groupChatFunction.SetGroupAsync(message, cancellationToken),
            { Text: "/unset_group" } => _groupChatFunction.UnsetGroupAsync(message, cancellationToken),
            { Text: "/send"} and {IsTopicMessage: true} => _groupChatFunction.SendingResponseAsync(message, cancellationToken),
            { Text: "/close_topic" } => _groupChatFunction.CloseTopicAsync(message, cancellationToken),
            { Text: "/topic_statistics" } => _statisticsFunction.TopicStatisticAsync(message, cancellationToken),
            { Text: var text } when text.StartsWith("/topic_statistics_date ") => _statisticsFunction.TopicStatisticByDateAsync(message, cancellationToken),
            { ReplyToMessage.Text: not null } => _groupChatFunction.ReplyToBotMessageAsync(message, cancellationToken),
            _ and {From.IsBot: false} => _client.SendTextMessageAsync(message.Chat, "I didn't understand u")
        };
        
        _logger.LogInformation("Command {@command} executed", func);
        await func;
    }

    private Task UnknownUpdate(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("The update: {@update} was not processed", update);
        return Task.CompletedTask;
    }

}