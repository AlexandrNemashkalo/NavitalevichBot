using NavitalevichBot.Actions.Core;
using NavitalevichBot.Models;
using System.Text.Json;
using Telegram.Bot;

namespace NavitalevichBot.Actions;

internal class SetSettingsAction : BaseUserAction, IBotAction
{
    private readonly LastUpdatesManager _lastUpdatesManager;
    private InstModuleManager _instModuleManager;

    public SetSettingsAction(
        LastUpdatesManager lastUpdatesManager,
        InstModuleManager instModuleManager,
        ITelegramBotClient botClient
    ) : base(botClient)
    {
        _lastUpdatesManager = lastUpdatesManager;
        _instModuleManager = instModuleManager;
    }

    public async Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default)
    {
        var updateMessage = data.Update.Message;
        var chatId = updateMessage!.Chat!.Id;

        if (updateMessage!.Text == BotActionCommands.setsettings) 
        {
            if (!await IsInstAuth(chatId, data.UserStatus, cancellationToken)) return true;
            await PreSetSettingsInfo(chatId, cancellationToken);
            return true;
        }

        if (_lastUpdatesManager.GetLastUpdateMessage(chatId) == BotActionCommands.setsettings)
        {
            if (!await IsInstAuth(chatId, data.UserStatus, cancellationToken)) return true;
            await SetSettings(chatId, updateMessage!.Text!, cancellationToken);
            return true;
        }

        return false;
    }

    private async Task PreSetSettingsInfo(long chatId, CancellationToken cancellationToken)
    {
        var instModule = _instModuleManager.GetInstModule(chatId);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var message = ""
            + $"⚙️ Send me the settings in the format below:\n"
            + $"\n"
            + $"{JsonSerializer.Serialize(instModule.Settings, options)}";

        await _botClient.SendTextMessageAsync(chatId, message, cancellationToken: cancellationToken);
    }

    private async Task SetSettings(long chatId, string message, CancellationToken cancellationToken)
    {
        var instModule = _instModuleManager.GetInstModule(chatId);
        InstModuleSettings settings = null;
        try
        {
            settings = JsonSerializer.Deserialize<InstModuleSettings>(message);
            if (settings == null)
            {
                throw new Exception();
            }
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(chatId, "error, uncorrect format", cancellationToken: cancellationToken);
            return;
        }

        settings.PostsCountPage = settings.PostsCountPage > 10 ? 10 : settings.PostsCountPage;
        settings.PostsCountPage = settings.PostsCountPage < 1 ? 1 : settings.PostsCountPage;
        settings.PostsPeriodHours = settings.PostsPeriodHours < 1 ? 1 : settings.PostsPeriodHours;
        settings.StoryPeriodHours = settings.StoryPeriodHours < 1 ? 1 : settings.StoryPeriodHours;

        instModule.Settings = settings;
        await _botClient.SendTextMessageAsync(chatId, "success", cancellationToken: cancellationToken);
    }
}
