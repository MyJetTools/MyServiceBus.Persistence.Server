using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.Metrics;

namespace MyServiceBus.Persistence.Domains.PersistenceOperations
{
    public class SyncAndGcBlobOperations
    {
        private readonly IMessagesContentPersistentStorage _messagesContentPersistentStorage;
        private readonly MessagesContentCache _messagesContentCache;
        private readonly TaskSchedulerByTopic _taskSchedulerByTopic;
        private readonly AppGlobalFlags _appGlobalFlags;
        private readonly MaxPersistedMessageIdByTopic _maxPersistedMessageIdByTopic;

        public SyncAndGcBlobOperations(IMessagesContentPersistentStorage messagesContentPersistentStorage,
            MessagesContentCache messagesContentCache, TaskSchedulerByTopic taskSchedulerByTopic, AppGlobalFlags appGlobalFlags, 
            MaxPersistedMessageIdByTopic maxPersistedMessageIdByTopic)
        {
            _messagesContentPersistentStorage = messagesContentPersistentStorage;
            _messagesContentCache = messagesContentCache;
            _taskSchedulerByTopic = taskSchedulerByTopic;
            _appGlobalFlags = appGlobalFlags;
            _maxPersistedMessageIdByTopic = maxPersistedMessageIdByTopic;
        }


        public async ValueTask SyncAndGc()
        {
            
            if (!_appGlobalFlags.Initialized)
                return;

            var topics = _messagesContentCache.GetTopics();

            foreach (var topic in topics)
            {
                await _taskSchedulerByTopic.ExecuteTaskAsync(topic, "Sync blobs", async ()=>
                {
                    
                    var messageId =  await _messagesContentPersistentStorage.SyncAsync(topic);
                    
                    if (messageId>0)
                        _maxPersistedMessageIdByTopic.Update(topic, messageId);
                    
                    
                });
    
            }

        }
        
    }
}