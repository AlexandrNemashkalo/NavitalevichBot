using NavitalevichBot.Data.Entities;
using NavitalevichBot.Data.Repositories;

namespace NavitalevichBot.Data.Mongo;
public class MongoStorageContext : IStorageContext
{
    private readonly IAvaliableChatsRepository _avaliableChatsRepository;
    private readonly IBlackListRepository _blackListRepository;
    private readonly ISeenMediaRepository _seenMediaRepository;
    private readonly ISeenStoryRepository _seenStoryRepository;
    private readonly ISessionMessageRepository _sessionMessageRepository;
    private readonly IStorageInitializer _storageInitializer;

    public MongoStorageContext(
        IAvaliableChatsRepository avaliableChatsRepository,
        IBlackListRepository blackListRepository,
        ISeenMediaRepository seenMediaRepository,
        ISeenStoryRepository seenStoryRepository,
        ISessionMessageRepository sessionMessageRepository,
        IStorageInitializer storageInitializer
    )
    {
        _avaliableChatsRepository = avaliableChatsRepository;
        _blackListRepository = blackListRepository;
        _seenMediaRepository = seenMediaRepository;
        _sessionMessageRepository = sessionMessageRepository;
        _seenStoryRepository = seenStoryRepository;
        _storageInitializer = storageInitializer;
    }

    public async Task AddAvaliableChatId(long chatId, string name)
    {
        await _avaliableChatsRepository.AddAvaliableChatId(chatId, name);
    }

    public async Task AddSeenMedia(List<SeenMedia> seenMedias)
    {
        await _seenMediaRepository.AddSeenMedia(seenMedias);
    }

    public async Task AddSeenStories(IEnumerable<long> storyIds, long chatId)
    {
        await _seenStoryRepository.AddSeenStories(storyIds, chatId);
    }

    public async Task AddSessionMessage(int messageId, long chatId)
    {
        await _sessionMessageRepository.AddSessionMessage(messageId, chatId);
    }

    public async Task AddUserToBlackList(string userName, long chatId)
    {
        await _blackListRepository.AddUserToBlackList(userName, chatId);
    }

    public async Task DeleteSessionMessage(long chatId)
    {
        await _sessionMessageRepository.DeleteSessionMessage(chatId);
    }

    public async Task<List<string>> GetBlackList(long chatId)
    {
        return await _blackListRepository.GetBlackList(chatId);
    }

    public async Task<string> GetMediaIdByMessageId(int messageId, long chatId)
    {
        return await _seenMediaRepository.GetMediaIdByMessageId(messageId, chatId);
    }

    public async Task<int?> GetSessionMessage(long chatId)
    {
        return await _sessionMessageRepository.GetSessionMessage(chatId);
    }

    public async Task<HashSet<long>> GetUnSeenStories(IEnumerable<long> storyIds, long chatId)
    {
        return await _seenStoryRepository.GetUnSeenStories(storyIds, chatId);
    }

    public async Task InitializeStorage()
    {
        await _storageInitializer.InitializeStorage();
    }

    public async Task<bool> IsAvaliableChatId(long chatId)
    {
        return await _avaliableChatsRepository.IsAvaliableChatId(chatId);
    }

    public async Task<bool> IsSeenMedia(string mediaId, long chatId)
    {
        return await _seenMediaRepository.IsSeenMedia(mediaId, chatId);
    }
}

