using MongoDB.Driver;
using NavitalevichBot.Data.Mongo.Entities;
using NavitalevichBot.Data.Repositories;

namespace NavitalevichBot.Data.Mongo.Repositories;

internal class AvaliableChatsRepository : IAvaliableChatsRepository
{
    private readonly MongoContext _context;

    public AvaliableChatsRepository(MongoContext context)
    {
        _context = context;
    }

    public async Task AddAvaliableChatId(long chatId, string name)
    {
        var avaliableChat = new AvaliableChatBson()
        {
            ChatId = chatId,
            Name = name
        };

        await _context.AvaliableChats.InsertOneAsync(avaliableChat);
    }

    public async Task<bool> IsAvaliableChatId(long chatId)
    {
        var avaliableChat = await _context.AvaliableChats.Find(x => x.ChatId == chatId).FirstOrDefaultAsync();
        return avaliableChat != null;
    }
}
