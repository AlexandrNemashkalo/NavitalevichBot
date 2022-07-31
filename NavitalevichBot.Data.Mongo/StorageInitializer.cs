using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using NavitalevichBot.Data.Repositories;

namespace NavitalevichBot.Data.Mongo;

internal class StorageInitializer : IStorageInitializer
{
    private readonly IConfiguration _config;
    private readonly IAvaliableChatsRepository _avaliableChatsRepository;

    public StorageInitializer(
        IConfiguration config,
        IAvaliableChatsRepository avaliableChatsRepository
    )
    {
        _config = config;
        _avaliableChatsRepository = avaliableChatsRepository;
    }

    public async Task InitializeStorage()
    {
        var client = new MongoClient(_config.GetSection("MongoConnectionString").Value);
        var database = client.GetDatabase(_config.GetSection("MongoDbName").Value);

        foreach (var collectionName in CollectionNames.KnownNames)
        {
            var collection = database.GetCollection<BsonDocument>(collectionName);
            if (collection == null)
            {
                await database.CreateCollectionAsync(collectionName);
            }
        }

        var isAvaliable = await _avaliableChatsRepository.IsAvaliableChatId(long.Parse(_config.GetSection("AdminChatId").Value));
        if (!isAvaliable)
        {
            await _avaliableChatsRepository.AddAvaliableChatId(long.Parse(_config.GetSection("AdminChatId").Value), _config.GetSection("AdminName").Value);
        }
    }
}
