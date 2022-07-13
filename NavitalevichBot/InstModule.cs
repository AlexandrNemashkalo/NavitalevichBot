using FluentScheduler;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace NavitalevichBot;

internal class InstModule : Registry
{
    private readonly DatabaseContext _dbContext;
    private readonly IInstaApi _instaApi;
    private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();
    private IMemoryCache _cache;
    private string GetPageKey() => "postsPage";


    private readonly ITelegramBotClient BotClient;
    private readonly long ChatId;

    public InstModuleSettings Settings { get; set; }

    public InstModule(
        IInstaApi instaApi, 
        ITelegramBotClient botClient,
        DatabaseContext dbContext,
        long chatId, 
        CancellationToken cancellationToken)
    {
        BotClient = botClient;
        _instaApi = instaApi;
        _dbContext = dbContext;
        ChatId = chatId;

        Settings = InstModuleSettings.Deffault;
        var options = new MemoryCacheOptions();
        _cache = new MemoryCache(options);

        SetPage(Settings.PostsCountPage);

        Schedule(async () => { if (Settings.IsGetPosts) { await SendPostsDo(Settings.PostsCountPage, cancellationToken); } })
            .WithName(nameof(SendPostsDo))
            .ToRunNow()
            .AndEvery(Settings.PostsPeriodHours).Hours();

        Schedule(async () => { if (Settings.IsGetStories) { await SendStoriesDo(cancellationToken); } })
            .WithName(nameof(SendStoriesDo))
            .ToRunNow()
            .AndEvery(Settings.StoryPeriodHours).Hours();

        Console.WriteLine("run");
    }

    public async Task GetInfo(CancellationToken cancellationToken)
    {
        var blackList = (await _dbContext.GetBlackList(ChatId)).ToHashSet();

        var message = ""
            + $"⚙️ Bot settings:\n"
            + $"get stories: {Settings.IsGetStories}\n"
            + $"period: {Settings.StoryPeriodHours} h\n"
            + $"\n"
            + $"get posts: {Settings.IsGetPosts}\n"
            + $"period: {Settings.PostsPeriodHours} h\n"
            + $"page: {Settings.PostsCountPage}\n"
            + $"\n"
            + $"🚫 Black list:\n"
            + $"{string.Join(", ", blackList)}";
        await BotClient.SendTextMessageAsync(ChatId, message, cancellationToken: cancellationToken);
    }
    public async Task PreSetSettingsInfo(CancellationToken cancellationToken)
    {
        var blackList = (await _dbContext.GetBlackList(ChatId)).ToHashSet();
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var message = ""
            + $"⚙️ Send me the settings in the format below:\n"
            + $"\n"
            + $"{JsonSerializer.Serialize(Settings, options)}";

        await BotClient.SendTextMessageAsync(ChatId, message, cancellationToken: cancellationToken);
    }

    public async Task SetSettings(string message, CancellationToken cancellationToken)
    {
        InstModuleSettings settings = null;
        try
        {
            settings = JsonSerializer.Deserialize<InstModuleSettings>(message);
            if(settings == null)
            {
                throw new Exception();
            }
        }
        catch (Exception ex)
        {
            await BotClient.SendTextMessageAsync(ChatId, "error, uncorrect format", cancellationToken: cancellationToken);
            return;
        }

        settings.PostsCountPage = settings.PostsCountPage > 10 ? 10 : settings.PostsCountPage;
        settings.PostsCountPage = settings.PostsCountPage < 1 ? 1 : settings.PostsCountPage;
        settings.PostsPeriodHours = settings.PostsPeriodHours < 1 ? 1 : settings.PostsPeriodHours;
        settings.StoryPeriodHours = settings.StoryPeriodHours < 1 ? 1 : settings.StoryPeriodHours;

        Settings = settings;
        await BotClient.SendTextMessageAsync(ChatId, "success", cancellationToken: cancellationToken);
    }

    public async Task LikeMedia(int? messageid, CancellationToken cancellationToken)
    {
        if(messageid == null)
        {
            await BotClient.SendTextMessageAsync(ChatId, "error, need reply message", cancellationToken: cancellationToken);
            return;
        }
        var mediaId = await _dbContext.GetMediaIdByMessageId(messageid.Value, ChatId);

        var result = await _instaApi.MediaProcessor.LikeMediaAsync(mediaId);
        if(result.Succeeded && result.Value)
        {
            await BotClient.SendTextMessageAsync(ChatId, "success", cancellationToken: cancellationToken);
        }
    }

    public async Task AddUserToBlackList(string userName, CancellationToken cancellationToken)
    {
        await _dbContext.AddUserToBlackList(userName, ChatId);
        await BotClient.SendTextMessageAsync(ChatId, "success", cancellationToken: cancellationToken);
    }

    public async Task ResetCache(CancellationToken cancellationToken)
    {
        if (_resetCacheToken != null && !_resetCacheToken.IsCancellationRequested && _resetCacheToken.Token.CanBeCanceled)
        {
            _resetCacheToken.Cancel();
            _resetCacheToken.Dispose();
        }

        _resetCacheToken = new CancellationTokenSource();
        SetPage(1);
        await BotClient.SendTextMessageAsync(ChatId, "success reset ceche", cancellationToken: cancellationToken);
    }

    public async Task SendStories(CancellationToken cancellationToken)
    {
        var result = await SendStoriesDo(cancellationToken);
        if (!result)
        {
            await BotClient.SendTextMessageAsync(ChatId, "the stories are over", cancellationToken: cancellationToken);
        }
    }

    public async Task SendPosts(CancellationToken cancellationToken)
    {
        int currentPage = 1; ;
        if (_cache.TryGetValue<int>(GetPageKey(), out var page))
        {
            currentPage = page + 1;
            SetPage(page + 1);
        }
        else
        {
            SetPage(1);
        }
        var result = await SendPostsDo(currentPage, cancellationToken);
        if (!result)
        {
            await BotClient.SendTextMessageAsync(ChatId, "the new posts are over", cancellationToken: cancellationToken);
        }
    }


    private async Task<bool> SendStoriesDo(CancellationToken cancellationToken)
    {
        var stories = await _instaApi.StoryProcessor.GetStoryFeedAsync();

        if (!stories.Succeeded)
        {
            Console.WriteLine(stories.Info.Message);
            await BotClient.SendTextMessageAsync(ChatId, "some error", cancellationToken: cancellationToken);
            return true;
        }

        var hasNewMedia = false;
        var blackList = (await _dbContext.GetBlackList(ChatId)).ToHashSet();
        var storyItemIds = stories.Value.Items.SelectMany(x => x.Items).Select(x => x.Pk);
        var unSeenStories = await _dbContext.GetUnSeenStories(storyItemIds, ChatId);
        foreach (var story in stories.Value.Items)
        {
            if (blackList.Contains(story.User.UserName))
            {
                continue;
            }

            var storiesList = new List<InstaStoryItem>();
            if (story.Items?.Any() == true)
            {
                storiesList = story.Items;
            }
            else{
                var userStories = await _instaApi.StoryProcessor.GetUserStoryAsync(story.User.Pk);
                if (!userStories.Succeeded)
                {
                    Console.WriteLine("Story Error: " + userStories.Info.Message);
                    continue;
                }
                storiesList = userStories.Value.Items;
            }
   
            var urs = new List<(bool, Uri)>();

            foreach (var storyItem in storiesList)
            {
                if (!unSeenStories.Contains(storyItem.Pk))
                {
                    continue;
                }

                if (storyItem.VideoList?.Any() == true && storyItem.HasAudio)
                {
                    urs.Add((false, new Uri(storyItem.VideoList.First().Uri)));
                }
                else if(storyItem.ImageList?.Any() == true)
                {
                    urs.Add((true, new Uri(storyItem.ImageList.First().Uri)));
                }
            }

            var caption = $"⚡️ {story.User.UserName}";
            var medias = GetAlbumInputMedias(urs, caption);

            if (medias?.Any() == true)
            {
                try
                {
                    var hasItems = true;
                    var i = 0;
                    while (hasItems)
                    {
                        var mediasPage = medias.Skip(i*10).Take(10);
                        if(mediasPage?.Any() == true)
                        {
                            i++;
                            await BotClient.SendMediaGroupAsync(ChatId, mediasPage);
                            hasNewMedia = true;
                            await Task.Delay(100);
                        }
                        else
                        {
                            hasItems = false;
                        }
                    };
                }
                catch (ApiRequestException ex)
                {
                    Console.WriteLine("Story error: " + ex.Message);
                    Console.WriteLine(ex.ErrorCode);
                }
            }
        }
        await _dbContext.AddSeenStories(unSeenStories, ChatId);
        return hasNewMedia;
    }

    private async Task<bool> SendPostsDo(int page, CancellationToken cancellationToken)
    {
        var newFeeds = await _instaApi.FeedProcessor.GetUserTimelineFeedAsync(
            InstagramApiSharp.PaginationParameters.MaxPagesToLoad(page));

        if (!newFeeds.Succeeded)
        {
            Console.WriteLine("Error GetUserTimelineFeedAsync: " + newFeeds.Info.Message);
            await BotClient.SendTextMessageAsync(ChatId, "inst error: " + newFeeds.Info.Message, cancellationToken: cancellationToken);
            return true;
        }
        if(newFeeds.Value.MediaItemsCount == 0)
        {
            return false;
        }

        var hasNewMedia = false;
        var blackList = (await _dbContext.GetBlackList(ChatId)).ToHashSet();
        var mediaIds = new List<string>();
        foreach (var media in newFeeds.Value.Medias)
        {
            if (blackList.Contains(media.User.UserName) || await _dbContext.IsSeenMedia(media.Pk, ChatId))
            {
                continue;
            }

            mediaIds.Add(media.Pk);
            var urs = new List<(bool, Uri)>();
            if (media.Carousel?.Any() == true)
            {
                foreach (var caruselItem in media.Carousel)
                {
                    if (caruselItem.MediaType == InstaMediaType.Video && caruselItem.Videos?.Any() == true )
                    {
                        urs.Add((false, new Uri(caruselItem.Videos.First().Uri)));
                    }
                    else if (caruselItem.MediaType == InstaMediaType.Image && caruselItem.Images?.Any() == true)
                    {
                        urs.Add((true, new Uri(caruselItem.Images.First().Uri)));
                    }
                }
            }
            if (media.MediaType == InstaMediaType.Video && media.Videos?.Any() == true)
            {
                urs.Add((false, new Uri(media.Videos.First().Uri)));
            }
            if (media.MediaType == InstaMediaType.Image && media.Images?.Any() == true)
            {
                urs.Add((true, new Uri(media.Images.First().Uri)));
            }

            var caption = $"🌎 {media.User.UserName}\n{media.Caption?.Text}\n🕔 {media.TakenAt.Day}.{media.TakenAt.Month} {media.TakenAt.Hour}:{media.TakenAt.Minute}";
            var medias = GetAlbumInputMedias(urs, caption);

            if (medias?.Any() == true)
            {
                try
                {
                    var response = await BotClient.SendMediaGroupAsync(ChatId, medias, cancellationToken:cancellationToken);

                    await _dbContext.AddMediaToMessage(response.First().MessageId, media.Pk, ChatId);
                    hasNewMedia = true;
                    await Task.Delay(100);
                }
                catch(ApiRequestException ex)
                {
                    try
                    {
                        var photos = medias.Where(x => x is InputMediaPhoto);
                        var response = await BotClient.SendMediaGroupAsync(ChatId, photos, cancellationToken: cancellationToken);
                        await _dbContext.AddMediaToMessage(response.First().MessageId, media.Pk, ChatId);
                        hasNewMedia = true;
                        await Task.Delay(100);
                    }
                    catch (ApiRequestException ex2)
                    {
                        Console.WriteLine("Post error" + ex2.Message);
                        Console.WriteLine(ex2.ErrorCode);
                    }
                }
            }
        }
        if (mediaIds.Any())
        {
            await _dbContext.AddSeenMedia(mediaIds, ChatId);
        }
        return hasNewMedia;
    }

    private static List<IAlbumInputMedia> GetAlbumInputMedias(List<(bool,Uri)> urs, string caption)
    {
        var medias = new List<IAlbumInputMedia>();
        var isFirst = true;
        foreach (var (isPhoto, uri) in urs)
        {
            var inputMedia = new InputMedia(uri.AbsoluteUri);
            if (isPhoto)
            {
                var photo = new InputMediaPhoto(inputMedia);
                if (isFirst)
                {
                    photo.Caption = caption;
                    isFirst = false;
                }
                medias.Add(photo);
            }
            else
            {
                var video = new InputMediaVideo(inputMedia);
                if (isFirst)
                {
                    video.Caption = caption;
                    isFirst = false;
                }
                medias.Add(video);
            }
        }
        return medias;
    }

    private void SetPage(int page)
    {
        var opt = new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        };
        opt.AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));
        _cache.Set(GetPageKey(), page, opt);
    }
}

