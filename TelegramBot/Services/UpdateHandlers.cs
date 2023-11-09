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

    private readonly IPrivateChatFunction _privateChatFunction;
    private readonly IGroupChatFunction _groupChatFunction;
    
    public UpdateHandlers(ITelegramBotClient client, 
        IDataContext context, 
        IPrivateChatFunction privateChatFunction, 
        IGroupChatFunction groupChatFunction)
    {
        _client = client;
        _context = context;
        _privateChatFunction = privateChatFunction;
        _groupChatFunction = groupChatFunction;
    }
    
    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _                                       => exception.ToString()
        };

        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            MessageId = update.Message.MessageId,
            IsBot = update.Message.From.IsBot,
            ChatId = update.Message.Chat.Id,
            ChatType = update.Message.Chat.Type,
            Message = update.Message.Text,
            Time = DateTime.Now.ToUniversalTime()
        };

        await _context.Activities.AddAsync(activity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
      
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
            { ReplyToMessage.Text: not null } => _groupChatFunction.ReplyToBotMessageAsync(message, cancellationToken),
            _ => _client.SendTextMessageAsync(message.Chat, "I didn't understand u")
        };

        await func;
    }

    private Task UnknownUpdate(Update update, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

}