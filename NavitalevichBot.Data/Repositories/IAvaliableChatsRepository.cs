namespace NavitalevichBot.Data.Repositories;

public interface IAvaliableChatsRepository
{
    Task AddAvaliableChatId(long chatId, string name);

    Task<bool> IsAvaliableChatId(long chatId);
}
