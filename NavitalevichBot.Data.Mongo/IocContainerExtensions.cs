using Autofac;
using NavitalevichBot.Data.Repositories;
using NavitalevichBot.Data.Mongo.Repositories;

namespace NavitalevichBot.Data.Mongo;

public static class IocContainerExtensions
{
    public static ContainerBuilder AddMongoDataAccess(this ContainerBuilder builder)
    {
        builder.RegisterType<MongoStorageContext>().As<IStorageContext>().SingleInstance();
        builder.RegisterType<MongoContext>().SingleInstance();

        builder.RegisterType<StorageInitializer>().As<IStorageInitializer>().SingleInstance();
        builder.RegisterType<AvaliableChatsRepository>().As<IAvaliableChatsRepository>().SingleInstance();
        builder.RegisterType<BlackListRepository>().As<IBlackListRepository>().SingleInstance();
        builder.RegisterType<SeenMediaRepository>().As<ISeenMediaRepository>().SingleInstance();
        builder.RegisterType<SeenStoryRepository>().As<ISeenStoryRepository>().SingleInstance();
        builder.RegisterType<SessionMessageRepository>().As<ISessionMessageRepository>().SingleInstance();

        return builder;
    }
}
