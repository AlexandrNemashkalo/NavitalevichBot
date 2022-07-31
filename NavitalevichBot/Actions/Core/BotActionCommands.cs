using Telegram.Bot.Types;

namespace NavitalevichBot.Actions.Core;

internal static class BotActionCommands
{
    //user
    public const string disable = "/disable";
    public const string getinfo = "/getinfo";
    public const string getposts = "/getposts";
    public const string getstories = "/getstories";
    public const string like = "/like";
    public const string logout = "/logout";
    public const string resetcache = "/resetcache";
    public const string setsettings = "/setsettings";
    public const string setuser = "/setuser";


    //admin
    public const string addchat = "/addchat";
    public const string getproxy = "/getproxy";
    public const string setproxy = "/setproxy";

    public static List<BotCommand> GetUserBotCommands()
    {
        return new List<BotCommand>()
        {
            new(){ Command = getposts, Description = "🌍 Получить ленту" },
            new(){ Command = getstories, Description = "⚡️Получить истории" },
            new(){ Command = like, Description = "♥️ Поставить лайк" },
            new(){ Command = setuser, Description = "🧔🏻‍♂️ Войти в аккаунт" },
            new(){ Command = getinfo, Description = "ℹ️ Инфо о боте" },
            new(){ Command = setsettings, Description = "⚙️ Поменять настройки" },
            new(){ Command = resetcache, Description = "💣 Сбросить кэш" },
            new(){ Command = logout, Description = "🚪Выйти из аккаунта" },
        };
    }
}
