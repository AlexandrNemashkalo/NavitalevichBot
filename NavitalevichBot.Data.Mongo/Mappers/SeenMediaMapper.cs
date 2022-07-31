using NavitalevichBot.Data.Entities;
using NavitalevichBot.Data.Mongo.Entities;

namespace NavitalevichBot.Data.Mongo.Mappers;

internal static class SeenMediaMapper
{
    public static SeenMedia ToEnity(SeenMediaBson source)
    {
        return new SeenMedia
        {
            ChatId = source.ChatId,
            MediaId = source.MediaId,
            MessageId = source.MessageId
        };
    }

    public static SeenMediaBson ToBson(SeenMedia source)
    {
        return new SeenMediaBson
        {
            ChatId = source.ChatId,
            MediaId = source.MediaId,
            MessageId = source.MessageId
        };
    }
}