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
    public long ChatId { get; }
    private string StateData { get; set; }

    public MessageSessionHandler(ITelegramBotClient botClient, IStorageContext dbContext, long chatId)
    {
        _botClient = botClient;
        _dbContext = dbContext;
        ChatId = chatId;
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
        var sessionMessageIdTask = _dbContext.GetSessionMessage(ChatId);
        sessionMessageIdTask.Wait();
        var sessionMessageId = sessionMessageIdTask.Result;
        if (sessionMessageId == null)
        {
            var sendMessageTask = _botClient.SendTextMessageAsync(ChatId, stateData);
            sendMessageTask.Wait();
            var message = sendMessageTask.Result;
            _dbContext.AddSessionMessage(message.MessageId, ChatId);

            var pinChatMessageTask = _botClient.PinChatMessageAsync(ChatId, message.MessageId);
            pinChatMessageTask.Wait();
        }
        else
        {
            var editMessageTask = _botClient.EditMessageTextAsync(ChatId, sessionMessageId.Value, stateData);
            editMessageTask.Wait();
        }
    }

    public StateData GetStateData()
    {

        var sessionMessageIdTask = _dbContext.GetSessionMessage(ChatId);
        sessionMessageIdTask.Wait();
        var sessionMessageId = sessionMessageIdTask.Result;
        if (sessionMessageId != null)
        {
            var forwardMessageTask = _botClient.ForwardMessageAsync(ChatId, ChatId, sessionMessageId.Value);
            forwardMessageTask.Wait();
            var message = forwardMessageTask.Result;

            var deleteMessageTask = _botClient.DeleteMessageAsync(ChatId, message.MessageId);
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

