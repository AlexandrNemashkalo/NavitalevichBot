using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using Microsoft.Extensions.Logging;
using NavitalevichBot.Helpers;
using NavitalevichBot.Services;
using NavitalevichBot.Services.Session;
using System.Text.Json;

namespace NavitalevichBot;

public class InstClientFactory
{
    private readonly ProxyManager _proxyManager;
    private readonly ILogger _logger;

    public InstClientFactory(
        ProxyManager proxyManager,
        ILoggerFactory loggerFactory
    )
    {
        _proxyManager = proxyManager;
        _logger = loggerFactory.CreateLogger<InstClientFactory>();
    }

    public async Task<IInstaApi> CreateAndLoginInstClient(string username, string password, IInstSessionHandler instSessionHandler)
    {
        var chatId = instSessionHandler.ChatId;

        _logger.LogInformation(chatId, $"{username} пытается авторизоваться");
        var userSession = UserSessionData.ForUsername(username).WithPassword(password);
        var isSucceeded = true;
        var instaApiBuilder = InstaApiBuilder.CreateBuilder()
            .SetUser(userSession)
            //.UseLogger(new DebugLogger(LogLevel.All))
            .SetRequestDelay(RequestDelay.FromSeconds(0, 2))
            //.SetSessionHandler(new FileSessionHandler() { FilePath = $"{username}_bot.bin" })
            .SetSessionHandler(instSessionHandler);

        var proxy = _proxyManager.GetProxy();
        if (proxy != null)
        {
            instaApiBuilder.UseHttpClientHandler(new HttpClientHandler { Proxy = proxy });
        }
        var instaApi = instaApiBuilder.Build();
        

        //Load session
        LoadSession(instaApi);
        if (!instaApi.IsUserAuthenticated)
        {
            // Call this function before calling LoginAsync
            await instaApi.SendRequestsBeforeLoginAsync();
            // wait 5 seconds
            await Task.Delay(5000);
            var logInResult = await instaApi.LoginAsync();
            _logger.LogInformation(chatId, $"{logInResult.Value} - {logInResult.Succeeded}");
            if (logInResult.Succeeded)
            {
                var result = await instaApi.SendRequestsAfterLoginAsync();
                _logger.LogInformation(chatId, $"result RequestsAfterLogin: {result}");
                SaveSession(instaApi);
            }
            else
            {
                isSucceeded = false;
                if (logInResult.Value == InstaLoginResult.ChallengeRequired)     // 1.ChallengeRequired
                {
                    var challenge = await instaApi.GetChallengeRequireVerifyMethodAsync();
                    _logger.LogInformation(chatId, "Challenge: " + JsonSerializer.Serialize(challenge));
                    if (challenge.Succeeded)
                    {
                        if (challenge.Value.SubmitPhoneRequired)
                        {
                            _logger.LogInformation(chatId, "Нужно номер телефона");
                        }
                        else
                        {
                            if (challenge.Value.StepData != null)
                            {
                                if (!string.IsNullOrEmpty(challenge.Value.StepData.PhoneNumber))
                                {
                                    _logger.LogInformation(chatId, "Нужно номер телефона");
                                }
                                if (!string.IsNullOrEmpty(challenge.Value.StepData.Email))
                                {
                                    _logger.LogInformation(chatId, "Нужно подтвердить email");
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation(chatId, "Ошибка логина");
                    }
                }
                else if (logInResult.Value == InstaLoginResult.TwoFactorRequired)
                {
                    _logger.LogInformation(chatId, "Нужно пройти двуфакторную аунтефикацию");
                }
                else
                {
                    if (logInResult.Info.Exception != null)
                    {
                        throw logInResult.Info.Exception;
                    }
                    _logger.LogInformation(chatId, "UnknownInstError");
                }
            }
        }

        _logger.LogInformation(chatId, $"Получилось ли залогиниться: {isSucceeded}");

        return isSucceeded ? instaApi : null;
    }

    void LoadSession(IInstaApi instaApi)
    {
        instaApi?.SessionHandler?.Load();
    }

    void SaveSession(IInstaApi instaApi)
    {
        if (instaApi == null)
            return;
        if (!instaApi.IsUserAuthenticated)
            return;
        instaApi.SessionHandler.Save();
    }
}

