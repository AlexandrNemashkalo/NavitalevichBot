using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using NavitalevichBot.Data;
using NavitalevichBot.Services.Session;
using Newtonsoft.Json;
using Telegram.Bot;

namespace NavitalevichBot;
internal class MessageSessionHandler : IInstSessionHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IStorageContext _dbContext;
    private readonly long _chatId;
    private string StateData { get; set; }

    public MessageSessionHandler(ITelegramBotClient botClient, IStorageContext dbContext, long chatId)
    {
        _botClient = botClient;
        _dbContext = dbContext;
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
        var sessionMessageIdTask = _dbContext.GetSessionMessage(_chatId);
        sessionMessageIdTask.Wait();
        var sessionMessageId = sessionMessageIdTask.Result;
        if (sessionMessageId == null)
        {
            var sendMessageTask = _botClient.SendTextMessageAsync(_chatId, stateData);
            sendMessageTask.Wait();
            var message = sendMessageTask.Result;
            _dbContext.AddSessionMessage(message.MessageId, _chatId);

            var pinChatMessageTask = _botClient.PinChatMessageAsync(_chatId, message.MessageId);
            pinChatMessageTask.Wait();
        }
        else
        {
            var editMessageTask = _botClient.EditMessageTextAsync(_chatId, sessionMessageId.Value, stateData);
            editMessageTask.Wait();
        }
    }

    public StateData GetStateData()
    {

        var sessionMessageIdTask = _dbContext.GetSessionMessage(_chatId);
        sessionMessageIdTask.Wait();
        var sessionMessageId = sessionMessageIdTask.Result;
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

