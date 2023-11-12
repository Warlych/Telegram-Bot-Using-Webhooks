using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Infrastructure.Domain;
using File = System.IO.File;

namespace TelegramBot.Application.Common;

public static class Helper
{
    public static async Task SendingDocumentAsync(ITelegramBotClient client, long? toChat, Topic toTopic, Message message, CancellationToken cancellationToken)
    {
        if (toChat == null && toTopic == null)
            throw new ArgumentNullException(@"Parameters ""toChat"" and ""toTopic"" is null. At least one of the parameters is required.");
        
        var filePath = $"/store/files/{message.Chat.Id}/" + message.Document.FileName;

        if (!File.Exists(filePath))
            await DownloadDocumentAsync(client, message, cancellationToken);

        using (var stream = File.OpenRead(filePath))
        {
            if (toTopic != null)
                await client.SendDocumentAsync(chatId: toTopic.GroupId,
                    messageThreadId: toTopic.TopicId,
                    document: new InputFileStream(stream, message.Document.FileName),
                    caption: message.Caption,
                    cancellationToken: cancellationToken);

            if (toChat != null)
                await client.SendDocumentAsync(chatId: toChat,
                    document: new InputFileStream(stream, message.Document.FileName),
                    caption: message.Caption,
                    cancellationToken: cancellationToken);
        }
    }
    
    public static async Task DownloadDocumentAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var destinationPath = $"/store/files/{message.Chat.Id}/";
        if(!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);

        var documentId = message.Document.FileId;
        var document = await client.GetFileAsync(documentId, cancellationToken: cancellationToken);
        var filePath = document.FilePath;

        using (var stream = File.Create(destinationPath + message.Document.FileName))
        {
            await client.DownloadFileAsync(filePath, stream);
        }
    }
    public static async Task<long> GetGroupIdAsync()
    {
        var filePath = Environment.CurrentDirectory + "/Properties/userSettings.json";

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        var groupId = Convert.ToInt64(data["Group"]);
        return groupId;
    }

    public static async Task<DateTime> ExtractDateAsync(string str)
    {
        var strs = str.Split(' ');

        if (DateTime.TryParseExact(strs[1], "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None,
                out DateTime result))
        {
            return result;
        }

        return DateTime.Now;
    }
}