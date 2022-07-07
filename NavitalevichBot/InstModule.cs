using FluentScheduler;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace NavitalevichBot;

internal class InstModule : Registry
{
    private readonly ITelegramBotClient _botClient;
    private readonly IInstaApi _instaApi;
    private readonly DatabaseContext _dbContext;
    private readonly long _chatId;
    private const int DefaultPage = 2;

    private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();
    private IMemoryCache _cache;
    private ConcurrentDictionary<bool, HashSet<string>> _blackList;

    private string GetStoryKey(string id) => $"storyid#{id}";
    private string GetPostKey(string id) => $"postId#{id}";
    private string GetPageKey() => "postsPage";

    public InstModule(
        IInstaApi instaApi, 
        ITelegramBotClient botClient,
        DatabaseContext dbContext,
        long chatId, 
        CancellationToken cancellationToken)
    {
        _botClient = botClient;
        _instaApi = instaApi;
        _dbContext = dbContext;
        _chatId = chatId;

        _blackList = new ConcurrentDictionary<bool, HashSet<string>>()
        {
            [true] = _dbContext.GetBlackList().ToHashSet(),
        };

        var options = new MemoryCacheOptions();
        _cache = new MemoryCache(options);

        SetPage(DefaultPage);
        Schedule(async () => { await SendPosts(DefaultPage, cancellationToken); }).WithName(nameof(SendPosts)).ToRunNow().AndEvery(1).Hours();
        Schedule(async () => { await SendStories(cancellationToken); }).WithName(nameof(SendStories)).ToRunNow().AndEvery(1).Hours();

        Console.WriteLine("run12345");
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

    public async Task LikeMedia(int messageid, CancellationToken cancellationToken)
    {
        var mediaId = _dbContext.GetMediaId(messageid);

        var result = await _instaApi.MediaProcessor.LikeMediaAsync(mediaId);
        if(result.Succeeded && result.Value)
        {
            await _botClient.SendTextMessageAsync(_chatId, "success", cancellationToken: cancellationToken);
        }
    }

    public async Task AddUserToBlackList(string userName, CancellationToken cancellationToken)
    {
        _dbContext.AddUserToBlackList(userName);
        _blackList[true].Add(userName);
        await _botClient.SendTextMessageAsync(_chatId, "success", cancellationToken: cancellationToken);
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
        await _botClient.SendTextMessageAsync(_chatId, "success reset ceche", cancellationToken: cancellationToken);
    }

    public async Task SendStories(CancellationToken cancellationToken)
    {
        var stories = await _instaApi.StoryProcessor.GetStoryFeedAsync();

        if (!stories.Succeeded)
        {
            Console.WriteLine(stories.Info.Message);
            return;
        }

        var hasNewMedia = false;
        foreach (var story in stories.Value.Items)
        {
            if (_blackList[true].Contains(story.User.UserName))
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
                if(_cache.Get(GetStoryKey(storyItem.Id)) != null)
                {
                    continue;
                }

                var opt = new MemoryCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
                };
                opt.AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));
                _cache.Set(GetStoryKey(storyItem.Id), true, opt);

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
                            await _botClient.SendMediaGroupAsync(_chatId, mediasPage);
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
        if (!hasNewMedia)
        {
            await _botClient.SendTextMessageAsync(_chatId, "the stories are over", cancellationToken: cancellationToken);
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
        await SendPosts(currentPage, cancellationToken);
    }


    private async Task SendPosts(int page, CancellationToken cancellationToken)
    {
        var newFeeds = await _instaApi.FeedProcessor.GetUserTimelineFeedAsync(
            InstagramApiSharp.PaginationParameters.MaxPagesToLoad(page));

        if (!newFeeds.Succeeded)
        {
            Console.WriteLine("Error GetUserTimelineFeedAsync: " + newFeeds.Info.Message);
            await _botClient.SendTextMessageAsync(_chatId, "inst error: " + newFeeds.Info.Message, cancellationToken: cancellationToken);
            return;
        }
        if(newFeeds.Value.MediaItemsCount == 0)
        {
            return;
        }

        Console.WriteLine($"Media posts count: {newFeeds.Value.MediaItemsCount}");
        var hasNewMedia = false;
        foreach (var media in newFeeds.Value.Medias)
        {
            if (_cache.Get(GetPostKey(media.Pk)) != null || _blackList[true].Contains(media.User.UserName))
            {
                continue;
            }

            var opt = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(2),
            };
            opt.AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));
            _cache.Set(GetPostKey(media.Pk), true, opt);
            
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
                    var response = await _botClient.SendMediaGroupAsync(_chatId, medias, cancellationToken:cancellationToken);

                    _dbContext.AddMediaToMessage(response.First().MessageId, media.Pk);
                    hasNewMedia = true;
                    await Task.Delay(100);
                }
                catch(ApiRequestException ex)
                {
                    try
                    {
                        var photos = medias.Where(x => x is InputMediaPhoto);
                        var response = await _botClient.SendMediaGroupAsync(_chatId, photos, cancellationToken: cancellationToken);
                        _dbContext.AddMediaToMessage(response.First().MessageId, media.Pk);
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
        if (!hasNewMedia)
        {
            await _botClient.SendTextMessageAsync(_chatId, "the new posts are over", cancellationToken: cancellationToken);
        }
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

}

