using MongoDB.Driver;
using NavitalevichBot.Data.Entities;
using NavitalevichBot.Data.Mongo.Mappers;
using NavitalevichBot.Data.Repositories;

namespace NavitalevichBot.Data.Mongo.Repositories;

internal class SeenMediaRepository : ISeenMediaRepository
{
    private readonly MongoContext _context;

    public SeenMediaRepository(MongoContext context)
    {
        _context = context;
    }

    public async Task<bool> IsSeenMedia(string mediaId, long chatId)
    {
        var seenMedia = await _context.SeenMedias.Find(x => x.MediaId == mediaId && x.ChatId == chatId).FirstOrDefaultAsync();
        return seenMedia != null;
    }

    public async Task<string> GetMediaIdByMessageId(int messageId, long chatId)
    {
        var seenMedia = await _context.SeenMedias.Find(x => x.MessageId == messageId && x.ChatId == chatId).FirstOrDefaultAsync();

        return seenMedia?.MediaId;
    }

    public async Task AddSeenMedia(List<SeenMedia> seenMedias)
    {
        await _context.SeenMedias.InsertManyAsync(seenMedias.Select(x => SeenMediaMapper.ToBson(x)));
    }
}
