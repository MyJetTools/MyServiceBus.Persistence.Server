using MyDependencies;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations;
using MyServiceBus.Persistence.Domains.IndexByMinute;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;

namespace MyServiceBus.Persistence.Domains
{
    public static class MyServiceBusServicesBinder
    {
        public static void BindMyServiceBusPersistenceServices(this IServiceRegistrator sr)
        {
            sr.Register<QueueSnapshotCache>();
            sr.Register<QueueSnapshotWriter>();
            sr.Register<MessagesContentCache>();
            sr.Register<MessagesContentReader>();
            sr.Register<TopicAndQueueInitializer>();
            sr.Register<PersistentOperationsScheduler>();
            sr.Register<ActivePagesWarmerAndGc>();
            
            sr.Register<AppGlobalFlags>();
            
            sr.Register<IAppLogger>(new AppLogger());
            
            sr.Register<CompressedMessagesUtils>();
            
            sr.Register<ServicesDisposer>();
            
            sr.Register<IndexByMinuteWriter>();
            
            sr.Register<PagesToCompressDetector>();
        }
        
    }
}