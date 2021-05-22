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
        private readonly IAppLogger _appLogger;

        public SyncAndGcBlobOperations(IMessagesContentPersistentStorage messagesContentPersistentStorage,
            MessagesContentCache messagesContentCache, TaskSchedulerByTopic taskSchedulerByTopic, AppGlobalFlags appGlobalFlags, 
            IAppLogger appLogger)
        {
            _messagesContentPersistentStorage = messagesContentPersistentStorage;
            _messagesContentCache = messagesContentCache;
            _taskSchedulerByTopic = taskSchedulerByTopic;
            _appGlobalFlags = appGlobalFlags;
            _appLogger = appLogger;
        }


        public async ValueTask Sync()
        {
            
            if (!_appGlobalFlags.Initialized)
                return;

            var topics = _messagesContentCache.GetTopics();

            foreach (var topicId in topics)
            {
                foreach (var page in _messagesContentCache.GetWritablePagesHasMessagesToUpload(topicId))
                {
                    await _taskSchedulerByTopic.ExecuteTaskAsync(topicId, page.PageId, "Upload messages to blob", async ()=>
                    {
                        var result = await _messagesContentPersistentStorage.SyncAsync(topicId, page.PageId);

                        while (result != SyncResult.Done)
                        {
                            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, topicId, "PageId: " + page.PageId.Value,
                                "There are messages to upload but no writer found. Creating one...");
                            await _messagesContentPersistentStorage.CreateNewPageAsync(topicId, page.PageId, page);
                            result = await _messagesContentPersistentStorage.SyncAsync(topicId, page.PageId);
                        }
 
                    });
                }
            }

        }
        
    }
}