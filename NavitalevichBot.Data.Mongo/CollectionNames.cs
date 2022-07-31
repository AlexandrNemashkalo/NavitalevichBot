namespace NavitalevichBot.Data.Mongo;

internal static class CollectionNames
{
    public const string AvaliableChats = "AvaliableChats";
    public const string BlackList = "BlackList";
    public const string SeenMedia = "SeenMedia";
    public const string SeenStories = "SeenStories";
    public const string SessionMessages = "SessionMessages";

    public static readonly List<string> KnownNames;

    static CollectionNames()
    {
        KnownNames = new List<string>()
        {
            AvaliableChats,
            BlackList,
            SeenMedia,
            SeenStories,
            SessionMessages
        };
    }
}
