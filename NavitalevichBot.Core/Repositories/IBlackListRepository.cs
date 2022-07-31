namespace NavitalevichBot.Data.Repositories;

public interface IBlackListRepository
{
    Task<List<string>> GetBlackList(long chatId);

    Task AddUserToBlackList(string userName, long chatId);

}
