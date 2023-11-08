using Newtonsoft.Json;

namespace TelegramBot.Application.Common;

public static class Helper
{
    public static async Task<long> GetGroupIdAsync()
    {
        var filePath = Environment.CurrentDirectory + "/Properties/userSettings.json";

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        var groupId = Convert.ToInt64(data["Group"]);
        return groupId;
    }
}