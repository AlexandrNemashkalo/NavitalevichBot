using MongoDB.Driver;
using NavitalevichBot.Data.Mongo.Entities;
using NavitalevichBot.Data.Repositories;

namespace NavitalevichBot.Data.Mongo.Repositories;

internal class SeenStoryRepository : ISeenStoryRepository
{
    private readonly MongoContext _context;

    public SeenStoryRepository(MongoContext context)
    {
        _context = context;
    }

    public async Task AddSeenStories(IEnumerable<long> storyIds, long chatId)
    {
        var seenStories = storyIds.Select(x => new SeenStoryBson
        {
            ChatId = chatId,
            StoryId = x
        }).ToList();

        await _context.SeenStories.InsertManyAsync(seenStories);
    }

    public async Task<HashSet<long>> GetUnSeenStories(IEnumerable<long> storyIds, long chatId)
    {
        var seenStories = await _context.SeenStories.Find(x => x.ChatId == chatId && storyIds.Contains(x.StoryId)).ToListAsync();
        var seenStoryIds = seenStories.Select(x => x.StoryId).ToHashSet();

        var result = storyIds!.Except(seenStoryIds);

        return result.ToHashSet();
    }
}
