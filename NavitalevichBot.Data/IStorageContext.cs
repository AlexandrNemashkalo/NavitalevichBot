using NavitalevichBot.Data.Repositories;

namespace NavitalevichBot.Data;

public interface IStorageContext :
    IAvaliableChatsRepository,
    IBlackListRepository,
    ISeenMediaRepository,
    ISeenStoryRepository,
    ISessionMessageRepository
{
}
