using Telegram.Bot.Types;

namespace NavitalevichBot.Models;

public record class BotActionParams
{
    public Update Update { get; init; }
    public TelegramUserStatus UserStatus { get; set; }
}
