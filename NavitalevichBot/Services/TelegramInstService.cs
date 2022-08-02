using Telegram.Bot.Types;
using NavitalevichBot.Data;
using Telegram.Bot;
using InstagramApiSharp.Classes.Models;
using Telegram.Bot.Exceptions;
using NavitalevichBot.Exceptions;
using NavitalevichBot.Data.Entities;
using Microsoft.Extensions.Logging;
using NavitalevichBot.Helpers;

namespace NavitalevichBot.Services;

public class TelegramInstService
{
    private readonly IStorageContext _dbContext;
    private readonly ITelegramBotClient _botClient;
    private readonly InstModuleManager _instModuleManager;
    private readonly ExceptionHandler _exceptionHandler;
    private readonly ILogger _logger;

    public TelegramInstService(
        IStorageContext dbContext, 
        ITelegramBotClient botClient,
        InstModuleManager instModuleManager,
        ExceptionHandler exceptionHandler,
        ILoggerFactory loggerFactory
    )
    {
        _dbContext = dbContext;
        _botClient = botClient;
        _instModuleManager = instModuleManager; 
        _exceptionHandler = exceptionHandler;
        _logger = loggerFactory.CreateLogger<TelegramInstService>();
    }

    public async Task LikeMedia(long chatId, int? messageid, CancellationToken cancellationToken)
    {
        var instModule = _instModuleManager.GetInstModule(chatId);

        if (messageid == null)
        {
            await _botClient.SendTextMessageAsync(chatId, "error, need reply message", cancellationToken: cancellationToken);
            return;
        }
        var mediaId = await _dbContext.GetMediaIdByMessageId(messageid.Value, chatId);

        var result = await instModule.InstClient.MediaProcessor.LikeMediaAsync(mediaId);
        if (result.Succeeded && result.Value)
        {
            await _botClient.SendTextMessageAsync(chatId, "success", cancellationToken: cancellationToken);
        }
    }

    public async Task<bool> SendStories(long chatId, CancellationToken cancellationToken)
    {
        var instModule = _instModuleManager.GetInstModule(chatId);
        var stories = await instModule.InstClient.StoryProcessor.GetStoryFeedAsync();

        if (!stories.Succeeded)
        {
            await _exceptionHandler.HandleException(chatId, new InstException(stories.Info.Message, stories.Info.ResponseType));
            return true;
        }

        var hasNewMedia = false;
        var blackList = (await _dbContext.GetBlackList(chatId)).ToHashSet();
        var storyItemIds = stories.Value.Items.SelectMany(x => x.Items).Select(x => x.Pk);

        var unSeenStories = new HashSet<long>();
        if (storyItemIds.Any())
        {
            unSeenStories = await _dbContext.GetUnSeenStories(storyItemIds, chatId);
        }
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
            else
            {
                var userStories = await instModule.InstClient.StoryProcessor.GetUserStoryAsync(story.User.Pk);
                if (!userStories.Succeeded)
                {
                    await _exceptionHandler.HandleException(chatId, new InstException(userStories.Info.Message, userStories.Info.ResponseType));
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
                else if (storyItem.ImageList?.Any() == true)
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
                        var mediasPage = medias.Skip(i * 10).Take(10);
                        if (mediasPage?.Any() == true)
                        {
                            i++;
                            await _botClient.SendMediaGroupAsync(chatId, mediasPage);
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
                    _logger.LogError(chatId, $"Story error: { ex.Message}; {ex.ErrorCode} ");
                }
            }
        }
        if (unSeenStories.Any())
        {
            await _dbContext.AddSeenStories(unSeenStories, chatId);
        }
        return hasNewMedia;
    }

    public async Task<bool> SendPosts(long chatId, int page, CancellationToken cancellationToken)
    {
        var instModule = _instModuleManager.GetInstModule(chatId);

        var newFeeds = await instModule.InstClient.FeedProcessor.GetUserTimelineFeedAsync(
            InstagramApiSharp.PaginationParameters.MaxPagesToLoad(page));

        if (!newFeeds.Succeeded)
        {
            await _exceptionHandler.HandleException(chatId, new InstException(newFeeds.Info.Message, newFeeds.Info.ResponseType));
            return true;
        }

        if (newFeeds.Value.MediaItemsCount == 0)
        {
            return false;
        }

        var hasNewMedia = false;
        var blackList = (await _dbContext.GetBlackList(chatId)).ToHashSet();
        var mediaIds = new List<string>();
        var seenMedias = new List<SeenMedia>();
        foreach (var media in newFeeds.Value.Medias)
        {
            if (blackList.Contains(media.User.UserName) || await _dbContext.IsSeenMedia(media.Pk, chatId))
            {
                continue;
            }

            mediaIds.Add(media.Pk);
            var urs = new List<(bool, Uri)>();
            if (media.Carousel?.Any() == true)
            {
                foreach (var caruselItem in media.Carousel)
                {
                    if (caruselItem.MediaType == InstaMediaType.Video && caruselItem.Videos?.Any() == true)
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
                    var response = await _botClient.SendMediaGroupAsync(chatId, medias, cancellationToken: cancellationToken);

                    seenMedias.Add(new SeenMedia { ChatId =chatId, MediaId = media.Pk , MessageId = response.First().MessageId });
                    hasNewMedia = true;
                    await Task.Delay(100);
                }
                catch (ApiRequestException ex)
                {
                    try
                    {
                        var photos = medias.Where(x => x is InputMediaPhoto);
                        var response = await _botClient.SendMediaGroupAsync(chatId, photos, cancellationToken: cancellationToken);
                        seenMedias.Add(new SeenMedia { ChatId = chatId, MediaId = media.Pk, MessageId = response.First().MessageId });
                        hasNewMedia = true;
                        await Task.Delay(100);
                    }
                    catch (ApiRequestException ex2)
                    {
                        _logger.LogError(chatId, $"Story error: { ex2.Message}; {ex2.ErrorCode} ");
                    }
                }
            }
        }
        if (mediaIds.Any())
        {
            await _dbContext.AddSeenMedia(seenMedias);
        }
        return hasNewMedia;
    }

    private static List<IAlbumInputMedia> GetAlbumInputMedias(List<(bool, Uri)> urs, string caption)
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
