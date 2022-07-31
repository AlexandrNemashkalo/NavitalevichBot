using Autofac;
using NavitalevichBot.Data.Repositories;

namespace NavitalevichBot.Data.Sqlite;

public static class IocContainerExtensions
{
    public static ContainerBuilder AddSqliteDataAccess(this ContainerBuilder builder)
    {
        builder.RegisterType<SqliteStorageContext>()
            .As<IStorageContext>()
            .As<IAvaliableChatsRepository>()
            .As<IBlackListRepository>()
            .As<ISeenMediaRepository>()
            .As<ISeenStoryRepository>()
            .As<ISessionMessageRepository>()
            .As<IStorageInitializer>()
            .SingleInstance();

        return builder;
    }
}
