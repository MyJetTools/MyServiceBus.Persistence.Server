using Microsoft.Extensions.DependencyInjection;
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
        public static void BindMyServiceBusPersistenceServices(this IServiceCollection sr)
        {
            sr.AddSingleton<QueueSnapshotCache>();
            sr.AddSingleton<QueueSnapshotWriter>();
            sr.AddSingleton<MessagesContentCache>();
            sr.AddSingleton<MessagesContentReader>();
            sr.AddSingleton<TopicAndQueueInitializer>();
            sr.AddSingleton<PersistentOperationsScheduler>();
            sr.AddSingleton<ActivePagesWarmerAndGc>();
            
            sr.AddSingleton<AppGlobalFlags>();
            
            sr.AddSingleton<IAppLogger>(new AppLogger());
            
            sr.AddSingleton<CompressedMessagesUtils>();
            
            sr.AddSingleton<ServicesDisposer>();
            
            sr.AddSingleton<IndexByMinuteWriter>();
            
            sr.AddSingleton<PagesToCompressDetector>();
        }
        
    }
}