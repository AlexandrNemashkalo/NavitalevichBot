using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NavitalevichBot.Data.Mongo.Entities;

public class SeenStoryBson
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }


    [BsonElement("chatId")]
    public long ChatId { get; set; }


    [BsonElement("storyId")]
    public long StoryId { get; set; }
}