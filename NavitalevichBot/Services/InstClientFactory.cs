using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using NavitalevichBot.Exceptions;
using NavitalevichBot.Helpers;
using NavitalevichBot.Services;
using NavitalevichBot.Services.Session;
using System.Text.Json;

namespace NavitalevichBot;

public class InstClientFactory
{
    private readonly ProxyManager _proxyManager;

    public InstClientFactory(ProxyManager proxyManager)
    {
        _proxyManager = proxyManager;
    }

    public async Task<IInstaApi> CreateAndLoginInstClient(string username, string password, IInstSessionHandler instSessionHandler)
    {        
        Console.WriteLine(username);
        var userSession = UserSessionData.ForUsername(username).WithPassword(password);
        var isSucceeded = true;
        var instaApiBuilder = InstaApiBuilder.CreateBuilder()
            .SetUser(userSession)
            //.UseLogger(new DebugLogger(LogLevel.All))
            .SetRequestDelay(RequestDelay.FromSeconds(0, 1))
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
            Console.WriteLine($"{logInResult.Value} - {logInResult.Succeeded}");
            if (logInResult.Succeeded)
            {
                var result = await instaApi.SendRequestsAfterLoginAsync();
                Console.WriteLine($"result RequestsAfterLogin: {result}");
                SaveSession(instaApi);
            }
            else
            {
                isSucceeded = false;
                if (logInResult.Value == InstaLoginResult.ChallengeRequired)     // 1.ChallengeRequired
                {
                    var challenge = await instaApi.GetChallengeRequireVerifyMethodAsync();
                    Console.WriteLine("Challenge: " + JsonSerializer.Serialize(challenge));
                    if (challenge.Succeeded)
                    {
                        if (challenge.Value.SubmitPhoneRequired)
                        {
                            Console.WriteLine("Нужно номер телефона");
                        }
                        else
                        {
                            if (challenge.Value.StepData != null)
                            {
                                if (!string.IsNullOrEmpty(challenge.Value.StepData.PhoneNumber))
                                {
                                    Console.WriteLine("Нужно номер телефона");
                                }
                                if (!string.IsNullOrEmpty(challenge.Value.StepData.Email))
                                {
                                    Console.WriteLine("Нужно подтвердить email");
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ошибка логина");
                    }
                }
                else if (logInResult.Value == InstaLoginResult.TwoFactorRequired)
                {
                    Console.WriteLine("Нужно пройти двуфакторную аунтефикацию");
                }
                else
                {
                    if (logInResult.Info.Exception != null)
                    {
                        throw logInResult.Info.Exception;
                    }
                    Console.WriteLine("UnknownInstError");
                }
            }
        }
        else
        {
            Console.WriteLine("Успешно залогинились");
        }
        Console.WriteLine($"isSucceeded: {isSucceeded}");
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

