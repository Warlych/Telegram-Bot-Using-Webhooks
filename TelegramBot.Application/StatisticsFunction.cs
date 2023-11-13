using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Application.Common;
using TelegramBot.Application.Interfaces;
using TelegramBot.Infrastructure.Domain;
using TelegramBot.Infrastructure.Domain.Enums;
using TelegramBot.Infrastructure.Interfaces;

namespace TelegramBot.Application;

public class StatisticsFunction : IStatisticsFunction
{
    private readonly ITelegramBotClient _client;
    private readonly IDataContext _context;
    private readonly ILogger<IStatisticsFunction> _logger;

    public StatisticsFunction(ITelegramBotClient client, IDataContext context, ILogger<IStatisticsFunction> logger)
    {
        _client = client;
        _context = context;
        _logger = logger;
    }

    public async Task TopicStatisticAsync(Message message, CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();

        var topics = _context.Topics
            .Include(t => t.TopicActivies)
            .Where(t => t.GroupId == groupId && t.ClosingDate == null)
            .ToList();
        
        _logger.LogInformation("Send topic statistics, array: {@topics}", topics);
        await SendingTopicStatisticByContextAsync(topics, message, cancellationToken);
    }

    public async Task TopicStatisticByDateAsync(Message message, CancellationToken cancellationToken)
    {
        var dateTime = await Helper.ExtractDateAsync(message.Text);

        var groupId = await Helper.GetGroupIdAsync();

        var topics = _context.Topics
            .Include(t => t.TopicActivies)
            .Where(t => t.GroupId == groupId && t.CreationDate >= dateTime.ToUniversalTime() && t.ClosingDate != null)
            .ToList();

        _logger.LogInformation("Send topic statistics, array: {@topics}", topics);
        await SendingTopicStatisticByContextAsync(topics, message, cancellationToken);
    }

    private async Task SendingTopicStatisticByContextAsync(ICollection<Topic> topics, Message message,
        CancellationToken cancellationToken)
    {
        var groupId = await Helper.GetGroupIdAsync();
        
        if (topics.Count == 0)
        {
            await _client.SendTextMessageAsync(chatId: groupId,
                text: "There are no statistics for this period. Perhaps you should specify an earlier dateю",
                cancellationToken: cancellationToken);
            
            return;
        }
        
        var topicStatisticModel = new TopicStatisticModel()
        {
            CountAsk = 0,
            CountAdvt = 0,
            CountNews = 0,
            MessageInAskTopic = 0,
            MessageInAdvtTopic = 0,
            MessageInNewsTopic = 0
        };

        foreach (var topic in topics)
        {
            if (topic.TopicType == TopicType.Ask)
            {
                topicStatisticModel.CountAsk++;
                topicStatisticModel.MessageInAskTopic += topic.TopicActivies.Count;
            }
            
            if (topic.TopicType == TopicType.Advt)
            {
                topicStatisticModel.CountAdvt++;
                topicStatisticModel.MessageInAdvtTopic += topic.TopicActivies.Count;
            }
            
            if (topic.TopicType == TopicType.News)
            {
                topicStatisticModel.CountNews++;
                topicStatisticModel.MessageInNewsTopic += topic.TopicActivies.Count;
            }
        }
        
        await _client.SendTextMessageAsync(chatId: groupId,
            text: $"Statistic for a {groupId}: \n" +
                  $"Topic is an ask type count: {topicStatisticModel.CountAsk}, messages: {topicStatisticModel.MessageInAskTopic} \n" +
                  $"Topic is an advt type count: {topicStatisticModel.CountAdvt}, messages: {topicStatisticModel.MessageInAdvtTopic} \n" +
                  $"Topic is an news type count: {topicStatisticModel.CountNews}, messages: {topicStatisticModel.MessageInNewsTopic} \n",
            cancellationToken: cancellationToken);
    }

    private class TopicStatisticModel
    {
        public int CountAsk { get; set; }
        public int CountAdvt { get; set; }
        public int CountNews { get; set; }
        
        public int MessageInAskTopic { get; set; }
        public int MessageInAdvtTopic { get; set; }
        public int MessageInNewsTopic { get; set; }
    }
}