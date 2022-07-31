using MongoDB.Bson.Serialization.Attributes;

namespace NavitalevichBot.Data.Mongo.Entities;

public class AvaliableChatBson
{
    [BsonId]
    [BsonElement("chatId")]
    public long ChatId { get; set; }


    [BsonElement("name")]
    public string Name { get; set; }
}