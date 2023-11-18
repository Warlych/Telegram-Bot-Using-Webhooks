using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Application.Common;
using TelegramBot.Application.Interfaces;
using TelegramBot.Exceptions;
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
    private readonly IChannelFunction _channelFunction;
    
    public UpdateHandlers(ITelegramBotClient client, 
        IDataContext context, 
        IPrivateChatFunction privateChatFunction, 
        IGroupChatFunction groupChatFunction,
        IStatisticsFunction statisticsFunction,
        IChannelFunction channelFunction,
        ILogger<UpdateHandlers> logger)
    {
        _client = client;
        _context = context;
        _logger = logger;
        _privateChatFunction = privateChatFunction;
        _groupChatFunction = groupChatFunction;
        _statisticsFunction = statisticsFunction;
        _channelFunction = channelFunction;
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
        _logger.LogInformation("New update: {@update}", update);

        var consumer = await CreateOrGetConsumerAsync(update, cancellationToken);

        if (consumer == null)
            throw new ArgumentNullException($"Consumer is null");
        
        await CreateActivityAsync(update, consumer, cancellationToken);
        
        var handler = update switch
        {
            { Message: { Chat.Type: ChatType.Private } message } => BotOnPrivateMessageReceiving(message,
                cancellationToken),
            { Message: { Chat.Type: ChatType.Group or ChatType.Supergroup } message } => BotOnGroupMessageReceiving(
                message, cancellationToken),
            { ChannelPost: not null } => BotOnChannelUpdateReceiving(update, cancellationToken),
            { Type: UpdateType.ChatMember } => BotOnNewChatMemberReceiving(update, cancellationToken),
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
        // A simple condition to ignore requests from third party groups. Validation may change depending on context
        var groupId = await Helper.GetGroupIdAsync();
            
        if(!groupId.Equals(0))
            if (groupId != message.Chat.Id)
                return;
        
        var func = message switch
        {
            { Text: "/start" } => _groupChatFunction.BeginAsync(message, cancellationToken),
            { Text: "/help" } => _groupChatFunction.HelpAsync(message, cancellationToken),
            { Text: "/set_group" } => _groupChatFunction.SetGroupAsync(message, cancellationToken),
            { Text: "/unset_group" } => _groupChatFunction.UnsetGroupAsync(message, cancellationToken),
            { Text: "/send"} and {IsTopicMessage: true} => _groupChatFunction.SendingResponseAsync(message, cancellationToken),
            { Text: "/close_topic" } => _groupChatFunction.CloseTopicAsync(message, cancellationToken),
            { Text: "/topic_statistics" } => _statisticsFunction.TopicStatisticAsync(message, cancellationToken),
            { Text: var text } when text.StartsWith("/ban") => _channelFunction.BanUserAsync(message, cancellationToken),
            { Text: var text } when text.StartsWith("/unban") => _channelFunction.UnbanUserAsync(message, cancellationToken),
            { Text: var text } when text.StartsWith("/topic_statistics_date ") => _statisticsFunction.TopicStatisticByDateAsync(message, cancellationToken),
            { Text: "/channel_members" } => _channelFunction.ChannelMemberCountAsync(message, cancellationToken),
            { Text: "/channel_subscribes" } => _statisticsFunction.ChannelSubscribeStatisticAsync(message, cancellationToken),
            { ReplyToMessage.Text: not null } => _groupChatFunction.ReplyToBotMessageAsync(message, cancellationToken),
            _ and {From.IsBot: false} => _client.SendTextMessageAsync(message.Chat, "I didn't understand u")
        };
        
        _logger.LogInformation("Command {@command} executed", func);
        await func;
    }

    private async Task BotOnChannelUpdateReceiving(Update update, CancellationToken cancellationToken)
    {
        // A simple condition to ignore requests from third party channels. Validation may change depending on context
        var channelId = await Helper.GetChannelIdAsync();
        if(!channelId.Equals(0))
            if (channelId != update.ChannelPost.Chat.Id)
                return;
        
        var func = update.ChannelPost switch
        {
            { Text: "/set_channel" } => _channelFunction.SetChannelAsync(update, cancellationToken),
            { Text: "/unset_channel" } => _channelFunction.UnsetChannelAsync(update, cancellationToken),
            _ => UnknownUpdate(update, cancellationToken),
        };

        _logger.LogInformation("Command {@command} executed", func);
        await func;
    }

    private async Task BotOnNewChatMemberReceiving(Update update, CancellationToken cancellationToken)
    {
        var channelId = await Helper.GetChannelIdAsync();

        if (update.ChatMember.Chat.Id == channelId && update.ChatMember.NewChatMember != null)
        {
            var newSubscribe = new GraphOfSubscribe()
            {
                Id = Guid.NewGuid(),
                ChannelId = channelId,
                EntryDate = DateTime.Now.ToUniversalTime(),
            };
            
            _logger.LogInformation("New subscribe from {@user} in {channelId}", update.ChatMember.From, channelId);
            
            await _context.Subscribes.AddAsync(newSubscribe);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
    
    private Task UnknownUpdate(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("The update: {@update} was not processed", update);
        return Task.CompletedTask;
    }

    private async Task<Consumer> CreateOrGetConsumerAsync(Update update, CancellationToken cancellationToken)
    {
        User user = null;

        switch (update.Type)
        {
            case UpdateType.Message:
                user = update.Message.From;
                break;
            
            case UpdateType.ChannelPost:
                user = update.ChannelPost.From;
                break;
            
            case UpdateType.ChatMember:
                user = update.ChatMember.NewChatMember.User;
                break;
            
            default:
                return null;
        }
        
        var consumer = await _context.Consumers
            .FirstOrDefaultAsync(c => c.ConsumerId == user.Id);

        if (consumer == null)
        {
            consumer = new Consumer()
            {
                ConsumerId = user.Id,
                Name = user.Username,
                IsBot = user.IsBot,
                EntryDate = DateTime.Now.ToUniversalTime(),
                Bans = new List<BanInfo>()
            };
            
            await _context.Consumers.AddAsync(consumer, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        
        _logger.LogInformation("Consumer created {@consumer}", consumer);
        return consumer;
    }
    
    private async Task CreateActivityAsync(Update update, Consumer consumer, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.ChatMember || update.Type == UpdateType.Unknown)
            return;
        
        if (update.Type == UpdateType.Message)
        {
            Topic topic = null;
            if (update.Message.MessageThreadId != null)
                topic = await _context.Topics
                    .FirstOrDefaultAsync(t => t.TopicId == update.Message.MessageThreadId);
            
            var activity = new Activity
            {
                Id = Guid.NewGuid(),
                ActivityFromBotId = update.Id,
                UpdateType = update.Type,
                ChatType = update.Message.Chat.Type,
                Text = update.Message.Text,
                Time = DateTime.Now.ToUniversalTime(),
                Topic = topic,
                Consumer = consumer
            };
            
            _logger.LogInformation("Activity created {@activity}", activity);
            
            await _context.Activities.AddAsync(activity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return;
        }

        if (update.Type == UpdateType.ChannelPost)
        {
            var activity = new Activity
            {
                Id = Guid.NewGuid(),
                ActivityFromBotId = update.Id,
                UpdateType = update.Type,
                ChatType = update.ChannelPost.Chat.Type,
                Text = update.ChannelPost.Text,
                Time = DateTime.Now.ToUniversalTime(),
                Topic = null,
                Consumer = consumer
            };
            
            _logger.LogInformation("Activity created {@activity}", activity);
            
            await _context.Activities.AddAsync(activity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            
            return;
        }
        
        throw new CannotCreateActivityException($"Failed to create activity for update type: {update.Type}");
    }
}