using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Android.DeviceInfo;
using InstagramApiSharp.Classes.SessionHandlers;
using InstagramApiSharp.Logger;
using System.Text.Json;

namespace NavitalevichBot;
internal class InstClientFactory
{
    public static async Task<IInstaApi> CreateAndLoginInstClient(string username, string password)
    {
        var userSession = UserSessionData.ForUsername(username).WithPassword(password);
        var isSucceeded = true;
        var instaApi = InstaApiBuilder.CreateBuilder()
            .SetUser(userSession)
            .SetDevice(AndroidDeviceGenerator.GetByName("honor-8lite"))
            //.UseLogger(new DebugLogger(LogLevel.All))
            .SetRequestDelay(RequestDelay.FromSeconds(0, 1))
            // Session handler, set a file path to save/load your state/session data
            .SetSessionHandler(new InstFileSessionHandler() { FilePath = $"{username}_bot.bin" })
            .Build();

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
                    Console.WriteLine("Login error: " + JsonSerializer.Serialize(logInResult.Info));
                    await instaApi.SendRequestsBeforeLoginAsync();

                    var isError = true;
                    while (isError)
                    {
                        await instaApi.SendRequestsBeforeLoginAsync();
                        await Task.Delay(10000);
                        var logInResult2 = await instaApi.LoginAsync();
                        Console.WriteLine($"Login error2 {logInResult2.Succeeded} : " + JsonSerializer.Serialize(logInResult2.Info));
                        isError = !logInResult2.Succeeded;
                    }
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

    static void LoadSession(IInstaApi instaApi)
    {
        instaApi?.SessionHandler?.Load();
    }

    static void SaveSession(IInstaApi instaApi)
    {
        if (instaApi == null)
            return;
        if (!instaApi.IsUserAuthenticated)
            return;
        instaApi.SessionHandler.Save();
    }

}

