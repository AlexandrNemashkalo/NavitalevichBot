using Autofac;
using Microsoft.Extensions.Configuration;
using NavitalevichBot.Actions;
using NavitalevichBot.Actions.AdminActions;
using NavitalevichBot.Data;
using NavitalevichBot.Data.Sqlite;
using NavitalevichBot.Services;
using Telegram.Bot;

namespace NavitalevichBot;

public static class IocContainerExtensions
{
    public static ContainerBuilder AddActions(this ContainerBuilder builder)
    {
        builder.RegisterType<SetUserAction>().As<IBotAction>().SingleInstance();
        builder.RegisterType<DisableInstUserAction>().As<IBotAction>().SingleInstance();
        builder.RegisterType<GetInfoAction>().As<IBotAction>().SingleInstance();
        builder.RegisterType<GetPostsAction>().As<IBotAction>().SingleInstance();
        builder.RegisterType<GetStoriesAction>().As<IBotAction>().SingleInstance();
        builder.RegisterType<LikePostAction>().As<IBotAction>().SingleInstance();
        builder.RegisterType<ResetCacheAction>().As<IBotAction>().SingleInstance();
        builder.RegisterType<SetSettingsAction>().As<IBotAction>().SingleInstance();
        builder.RegisterType<SetUserAction>().As<IBotAction>().SingleInstance();
        builder.RegisterType<LogoutUserAction>().As<IBotAction>().SingleInstance();

        builder.RegisterType<AddChatAction>().As<IBotAction>().SingleInstance();
        builder.RegisterType<GetProxyAction>().As<IBotAction>().SingleInstance();
        builder.RegisterType<SetProxyAction>().As<IBotAction>().SingleInstance();

        return builder;
    }

    public static ContainerBuilder AddServices(this ContainerBuilder builder)
    {
        builder.RegisterType<Application>().SingleInstance();
        builder.RegisterType<MessageSessionHandlerManager>().As<IInstSessionHandlerManager>().SingleInstance();
        builder.RegisterType<InstModuleManager>().SingleInstance();
        builder.RegisterType<TelegramInstService>().SingleInstance();
        builder.RegisterType<InstClientFactory>().SingleInstance();
        builder.RegisterType<AuthService>().SingleInstance();
        builder.RegisterType<ProxyManager>().SingleInstance();
        builder.RegisterType<LastUpdatesManager>().SingleInstance();
        builder.RegisterType<ExceptionHandler>().SingleInstance();

        return builder;
    }

    public static IConfiguration AddAndGetConfiguration(this ContainerBuilder builder)
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("appsettings.json");

        var config = configBuilder.Build();

        builder.RegisterInstance(config).As<IConfiguration>().SingleInstance();

        return config;
    }

    public static ContainerBuilder AddTelegramBotClient(this ContainerBuilder builder, IConfiguration config)
    {
        var token = config.GetSection("TelegramBotToken").Value;
        builder.RegisterInstance<ITelegramBotClient>(new TelegramBotClient(token)).SingleInstance();
  
        return builder;
    }
}
