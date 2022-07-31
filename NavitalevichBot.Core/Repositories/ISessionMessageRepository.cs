namespace NavitalevichBot.Data.Repositories;

public interface ISessionMessageRepository
{
    Task AddSessionMessage(int messageId, long chatId);

    Task<int?> GetSessionMessage(long chatId);

    Task DeleteSessionMessage(long chatId);
}