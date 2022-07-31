using MongoDB.Driver;
using NavitalevichBot.Data.Mongo.Entities;
using NavitalevichBot.Data.Repositories;

namespace NavitalevichBot.Data.Mongo.Repositories;

internal class SessionMessageRepository : ISessionMessageRepository
{
    private readonly MongoContext _context;

    public SessionMessageRepository(MongoContext context)
    {
        _context = context;
    }

    public async Task AddSessionMessage(int messageId, long chatId)
    {
        var sessionMessage = new SessionMessageBson
        {
            MessageId = messageId,
            ChatId = chatId
        };

        await _context.SessionMessages.InsertOneAsync(sessionMessage);
    }

    public async Task DeleteSessionMessage(long chatId)
    {
        await _context.SessionMessages.DeleteOneAsync(Builders<SessionMessageBson>.Filter.Eq("chatId", chatId));
    }

    public async Task<int?> GetSessionMessage(long chatId)
    {
        var sessionMessage = await _context.SessionMessages.Find(x => x.ChatId == chatId).FirstOrDefaultAsync();

        return sessionMessage?.MessageId;
    }
}
