namespace NavitalevichBot.Data.Repositories;

public interface ISeenStoryRepository
{
    Task AddSeenStories(IEnumerable<long> storyIds, long chatId);

    Task<HashSet<long>> GetUnSeenStories(IEnumerable<long> storyIds, long chatId);
}
