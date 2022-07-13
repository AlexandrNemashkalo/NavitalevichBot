using FluentScheduler;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
namespace NavitalevichBot;

public class Program
{
    static bool IsLocal = false;
    static bool IsMultiple = false;
    static ITelegramBotClient botClient { get; set; } = new TelegramBotClient(Constants.TelegramToken);

    private static ConcurrentDictionary<long, InstModule> InstModuleDict = new ConcurrentDictionary<long, InstModule>();
    private static ConcurrentDictionary<long, string> LastUpdateDict = new ConcurrentDictionary<long, string>();

    private static DatabaseContext _context;

    private static List<(Func<Update, bool>, Func<InstModule, Update, CancellationToken,Task>)> Actions = new List<(Func<Update, bool>, Func<InstModule, Update, CancellationToken,Task>)>()
    {
        (update => update.Message.Text == "/getposts", async (instModule, update, token) => await instModule.SendPosts(token)),
        (update => update.Message.Text == "/getstories", async (instModule, update, token) => await instModule.SendStories(token)),
        (update => update.Message.Text == "/resetcache", async (instModule, update, token) => await instModule.ResetCache(token)),
        (update => update.Message.Text == "/getinfo", async (instModule, update, token) => await instModule.GetInfo(token)),
        (update => update.Message.Text == "/like", async (instModule, update, token) => await instModule.LikeMedia(update.Message?.ReplyToMessage?.MessageId, token)),
        (update => update.Message.Text.StartsWith("/disable"), async (instModule, update, token) => await instModule.AddUserToBlackList(update.Message.Text.Split(" ")[1], token)),

        (update => update.Message.Text == "/setsettings", async (instModule, update, token) => await instModule.PreSetSettingsInfo(token)),
        (update => LastUpdateDict.GetValueOrDefault(update.Message.Chat.Id) == "/setsettings", async (instModule, update, token) => await instModule.SetSettings(update.Message.Text,token)),

        (update => update.Message.Text == "/addchat" && update.Message.Chat.Id == Constants.AdminChatId, async (instModule, update, token) => await PreAddChatId(token)),
        (update => LastUpdateDict.GetValueOrDefault(update.Message.Chat.Id) == "/addchat" && update.Message.Chat.Id == Constants.AdminChatId, async (instModule, update, token) => await AddChatId(update.Message.Text, token)),
    };

    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var chatId = update?.Message?.Chat?.Id;
        Console.WriteLine($"{ chatId} {DateTime.Now}");

        if (chatId == null) return;
        if (update.Type != UpdateType.Message) return;
        if (update.Message!.Type != MessageType.Text) return;

        try
        {
            var messageText = update.Message.Text;
            if (!await _context.IsAvaliableChatId(chatId.Value))
            {
                await botClient.SendTextMessageAsync(chatId, $"😩 sorry access blocked \n contact the bot admin (@navitalevich) and tell him your chatId:{chatId}", cancellationToken: cancellationToken);
            }

            if (!InstModuleDict.TryGetValue(chatId.Value, out var instModule) )
            {
                var lastMessage = LastUpdateDict.GetValueOrDefault(chatId.Value);
                LastUpdateDict[chatId.Value] = messageText;

                var isAuth = false;
                if (_context.GetSessionMessage(chatId.Value) != null)
                {
                    var instSessionHandler = new InstSessionHandler(botClient, _context, chatId.Value);
                    var instaApi = await InstClientFactory.CreateAndLoginInstClient(null, null, instSessionHandler);
                    if (instaApi != null)
                    {
                        var newInstModule = new InstModule(instaApi, botClient, _context, chatId.Value, cancellationToken);
                        JobManager.Initialize(newInstModule);
                        if (InstModuleDict.TryAdd(chatId.Value, newInstModule))
                        {
                            isAuth = true;
                        }
                    }
                }

                if (isAuth)
                {
                    // не разрешаем логинится, даем возможность выполнить действия
                }
                else if (messageText == "/setuser")
                {
                    var userdata = new UserData()
                    {
                        UserName = "example",
                        Password = "1234"
                    };
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    await botClient.SendTextMessageAsync(chatId, $"send me your userdata by format: \n{JsonSerializer.Serialize(userdata, options)}", cancellationToken: cancellationToken);
                    return;
                }
                else if (lastMessage == "/setuser")
                {
                    UserData userData = null;
                    try
                    {
                        userData = JsonSerializer.Deserialize<UserData>(messageText);
                        if(userData == null)
                        {
                            throw new Exception();
                        }
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendTextMessageAsync(chatId, "uncorrect data, try again /setuser later", cancellationToken: cancellationToken);
                        return;
                    }

                    var instSessionHandler = new InstSessionHandler(botClient, _context, chatId.Value);
                    var instaApi = await InstClientFactory.CreateAndLoginInstClient(userData.UserName, userData.Password, instSessionHandler);
                    if(instaApi == null)
                    {
                        await botClient.SendTextMessageAsync(chatId, "😢 authentication error, try disabling two-factor authentication and confirm the current address in the app", cancellationToken: cancellationToken);
                        return;
                    }
                    var newInstModule = new InstModule(instaApi, botClient, _context, chatId.Value, cancellationToken);
                    JobManager.Initialize(newInstModule);
                    if (InstModuleDict.TryAdd(chatId.Value, newInstModule))
                    {
                        await botClient.SendTextMessageAsync(chatId, "success", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "error, try again later", cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "you need to login to instagram account, send /setuser", cancellationToken: cancellationToken);
                    return;
                }
            }

            var instmod = InstModuleDict.GetValueOrDefault(chatId.Value);
            foreach (var (isMatch, action) in Actions)
            {
                if (instmod != null && isMatch(update))
                {
                    await action(instmod, update, cancellationToken);
                    LastUpdateDict[chatId.Value] = messageText;
                    return;
                }
            }

            LastUpdateDict[chatId.Value] = messageText;
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(chatId, $"some error, {ex.Message}", cancellationToken: cancellationToken);
        }
    }

    public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        string errorMessage = null;
        if (exception is ApiRequestException apiRequestException)
        {
            if(apiRequestException.Message == "Conflict: terminated by other getUpdates request; make sure that only one bot instance is running")
            {
                IsMultiple = true;
            }
            errorMessage = $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}";
        }
        else
        {
            errorMessage = exception.ToString();
        }
     
        Console.WriteLine($"{errorMessage}  {DateTime.Now}");
        return Task.CompletedTask;
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Запущен бот ");

        var path = Directory.GetCurrentDirectory();
        IsLocal = path == "C:\\Git\\NavitalevichBot\\NavitalevichBot\\bin\\Debug\\net6.0";

        using var cts = new CancellationTokenSource();

        _context = new DatabaseContext();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { } 
        };
        
        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token);

        var me = await botClient.GetMeAsync();
        
        Console.ReadLine();

        cts.Cancel();

        await Task.Delay(2000);
    }

    public static async Task AddChatId(string message, CancellationToken cancellationToken)
    {
        var chatInfo = message?.Split(" ");

        if(chatInfo != null && chatInfo.Length == 2 && long.TryParse(chatInfo[0], out var chatId))
        {
            _context.AddAvaliableChatId(chatId, chatInfo[1]);
            await botClient.SendTextMessageAsync(chatId, "🥳 the admin has allowed you to use the bot", cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendTextMessageAsync(Constants.AdminChatId, "uncorrect data, try again /addchat later", cancellationToken: cancellationToken);
        }
    }

    public static async Task PreAddChatId(CancellationToken cancellationToken)
    {
        await botClient.SendTextMessageAsync(Constants.AdminChatId, "send me your chat id and name separated by space", cancellationToken: cancellationToken);
    }
}