using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.SessionHandlers;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace NavitalevichBot;
internal class InstSessionHandler : ISessionHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly DatabaseContext _context;
    private readonly long _chatId;
    private string StateData { get; set; }

    public InstSessionHandler(ITelegramBotClient botClient, DatabaseContext context, long chatId)
    {
        _botClient = botClient;
        _context = context;
        _chatId = chatId;
    }

    public IInstaApi InstaApi { get; set; }
    public string FilePath { get; set; }

    public void Load()
    {
        if(StateData != null)
        {
            InstaApi.LoadStateDataFromString(StateData);
        }
    }

    public void Save()
    {
        var stateData = InstaApi.GetStateDataAsString();
        var sessionMessageId = _context.GetSessionMessage(_chatId);
        if(sessionMessageId == null)
        {
            var sendMessageTask = _botClient.SendTextMessageAsync(_chatId, stateData);
            sendMessageTask.Wait();
            var message = sendMessageTask.Result;
            _context.AddSessionMessage(message.MessageId, _chatId);
        }
        else
        {
            var editMessageTaask = _botClient.EditMessageTextAsync(_chatId, sessionMessageId.Value, stateData);
            editMessageTaask.Wait();
        }
    }

    public StateData GetStateData()
    {
        var sessionMessageId = _context.GetSessionMessage(_chatId);
        if (sessionMessageId != null)
        {
            var forwardMessageTask = _botClient.ForwardMessageAsync(_chatId, _chatId, sessionMessageId.Value);
            forwardMessageTask.Wait();
            var message = forwardMessageTask.Result;

            var deleteMessageTask = _botClient.DeleteMessageAsync(_chatId, message.MessageId);
            deleteMessageTask.Wait();
            if (message != null)
            {
                StateData = message.Text;
                return JsonConvert.DeserializeObject<StateData>(message.Text);
            }
        }
        return null;
    }
}

