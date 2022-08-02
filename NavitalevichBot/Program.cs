using Autofac;
using NavitalevichBot.Data.Mongo;

namespace NavitalevichBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        var containerBuilder = new ContainerBuilder();
        var config = containerBuilder.AddAndGetConfiguration();

		var container = containerBuilder
            .AddTelegramBotClient(config)
            .AddServices()
            .AddActions()
            //.AddSqliteDataAccess()
            .AddMongoDataAccess()
            .ConfigureLogging(config)
            .Build();

        await container.Resolve<Application>().Run(cts.Token);

        Console.ReadLine();
        cts.Cancel();
        await Task.Delay(2000);
        Console.WriteLine("Бот остановлен");
    }
}