using Autofac;
using NavitalevichBot.Data.Mongo;
using NavitalevichBot.Data.Sqlite;

namespace NavitalevichBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Бот запущен");

        using var cts = new CancellationTokenSource();

        var containerBuilder = new ContainerBuilder();
        var config = containerBuilder.AddAndGetConfiguration();

        var container = containerBuilder
            .AddTelegramBotClient(config)
            .AddServices()
            .AddActions()
            //.AddSqliteDataAccess()
            .AddMongoDataAccess()
            .Build();

        await container.Resolve<Application>().Run(cts.Token);

        Console.ReadLine();
        cts.Cancel();
        await Task.Delay(2000);
        Console.WriteLine("Бот остановлен");
    }
}