using MongoDB.Bson.Serialization.Attributes;

namespace NavitalevichBot.Data.Mongo.Entities;

public class SessionMessageBson
{

    [BsonId]
    [BsonElement("chatId")]
    public long ChatId { get; set; }


    [BsonElement("messageId")]
    public int MessageId { get; set; }
}
