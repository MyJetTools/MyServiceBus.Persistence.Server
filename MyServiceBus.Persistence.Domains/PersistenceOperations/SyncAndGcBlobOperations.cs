using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.MessagesContent;

namespace MyServiceBus.Persistence.Domains.PersistenceOperations
{
    public class SyncAndGcBlobOperations
    {
        private readonly IMessagesContentPersistentStorage _messagesContentPersistentStorage;
        private readonly MessagesContentCache _messagesContentCache;
        private readonly TaskSchedulerByTopic _taskSchedulerByTopic;
        private readonly AppGlobalFlags _appGlobalFlags;

        public SyncAndGcBlobOperations(IMessagesContentPersistentStorage messagesContentPersistentStorage,
            MessagesContentCache messagesContentCache, TaskSchedulerByTopic taskSchedulerByTopic, AppGlobalFlags appGlobalFlags)
        {
            _messagesContentPersistentStorage = messagesContentPersistentStorage;
            _messagesContentCache = messagesContentCache;
            _taskSchedulerByTopic = taskSchedulerByTopic;
            _appGlobalFlags = appGlobalFlags;
        }


        public async ValueTask SyncAndGc()
        {
            
            if (!_appGlobalFlags.Initialized)
                return;

            var topics = _messagesContentCache.GetTopics();

            foreach (var topic in topics)
            {
                await _taskSchedulerByTopic.ExecuteTaskAsync(topic, "Sync blobs", ()=>_messagesContentPersistentStorage.SyncAsync(topic));
    
            }

        }
        
    }
}