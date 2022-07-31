using NavitalevichBot.Data.Entities;

namespace NavitalevichBot.Data.Repositories;

public interface ISeenMediaRepository
{
    Task<bool> IsSeenMedia(string mediaId, long chatId);

    Task AddSeenMedia(List<SeenMedia> seenMedias);

    Task<string> GetMediaIdByMessageId(int messageId, long chatId);
}