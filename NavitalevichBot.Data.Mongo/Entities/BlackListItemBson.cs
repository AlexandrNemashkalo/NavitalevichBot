using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NavitalevichBot.Data.Mongo.Entities;

public class BlackListItemBson
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }


    [BsonElement("chatId")]
    public long ChatId { get; set; }


    [BsonElement("userName")]
    public string UserName { get; set; }
}
