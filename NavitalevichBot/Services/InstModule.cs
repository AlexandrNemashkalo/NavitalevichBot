using FluentScheduler;
using InstagramApiSharp.API;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using NavitalevichBot.Services;

namespace NavitalevichBot;

public class InstModule : Registry
{
    public readonly IInstaApi InstClient;
    public IMemoryCache Cache;
    public readonly long ChatId;
    public InstModuleSettings Settings { get; set; }

    private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();
    private static ExceptionHandler _exceptionHandler;
    public string GetPageKey() => "postsPage";

    public InstModule(
        IInstaApi instaApi, 
        long chatId, 
        ExceptionHandler exceptionHandler,
        CancellationToken cancellationToken
    )
    {
        _exceptionHandler = exceptionHandler;

        InstClient = instaApi;
        ChatId = chatId;
        Settings = InstModuleSettings.Deffault;

        var options = new MemoryCacheOptions();
        Cache = new MemoryCache(options);

        SetPage(Settings.PostsCountPage);
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
    }

    public void SetPage(int page)
    {
        var opt = new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        };
        opt.AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));
        Cache.Set(GetPageKey(), page, opt);
    }
}

