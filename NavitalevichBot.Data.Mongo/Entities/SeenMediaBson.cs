using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NavitalevichBot.Data.Mongo.Entities;

public class SeenMediaBson
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }


    [BsonElement("chatId")]
    public long ChatId { get; set; }


    [BsonElement("mediaId")]
    public string MediaId { get; set; }


    [BsonElement("messageId")]
    public long MessageId { get; set; }
}
