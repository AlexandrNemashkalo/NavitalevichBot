using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using NavitalevichBot.Data.Mongo.Entities;

namespace NavitalevichBot.Data.Mongo;

public class MongoContext
{
    private readonly IMongoDatabase _database;

    public MongoContext(IConfiguration config)
    {
        var client = new MongoClient(config.GetSection("MongoConnectionString").Value);
        _database = client.GetDatabase(config.GetSection("MongoDbName").Value);
    }

    public IMongoCollection<BlackListItemBson> BlackList
        => _database.GetCollection<BlackListItemBson>(CollectionNames.BlackList);

    public IMongoCollection<AvaliableChatBson> AvaliableChats
        => _database.GetCollection<AvaliableChatBson>(CollectionNames.AvaliableChats);

    public IMongoCollection<SeenStoryBson> SeenStories
        => _database.GetCollection<SeenStoryBson>(CollectionNames.SeenStories);

    public IMongoCollection<SeenMediaBson> SeenMedias
        => _database.GetCollection<SeenMediaBson>(CollectionNames.SeenMedia);

    public IMongoCollection<SessionMessageBson> SessionMessages
        => _database.GetCollection<SessionMessageBson > (CollectionNames.SessionMessages);
}
