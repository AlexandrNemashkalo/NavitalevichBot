using InstagramApiSharp.Classes.SessionHandlers;
using NavitalevichBot.Services.Session;

namespace NavitalevichBot.Services;

public interface IInstSessionHandlerManager
{
    IInstSessionHandler CreateSessionHandler(long chatId);
}
