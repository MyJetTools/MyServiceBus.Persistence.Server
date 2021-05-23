using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

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

            var topics = _messagesContentCache.GetLoadedPages();
            
            
            
            

            foreach (var (topicId, pages) in topics)
            {

                if (_appGlobalFlags.DebugTopic == topicId)
                    _appLogger.AddLog(LogProcess.Debug, topicId, "Sync Topic Event", $"Found pages {pages.Count} to sync ");

                
                foreach (var page in pages)
                {
                    
                    if (page is WritableContentPage {NotSavedAmount: > 0} writableContentCachePage)
                    {
                        if (_appGlobalFlags.DebugTopic == topicId)
                            _appLogger.AddLog(LogProcess.Debug, topicId, $"Sync Page {page.PageId}", $"Page with hash {page.GetHashCode()} has {page.NotSavedAmount} unsynched messages");
                        
                        await _taskSchedulerByTopic.ExecuteTaskAsync(topicId, page.PageId, "Upload messages to blob", async ()=>
                        {
                            
                            if (_appGlobalFlags.DebugTopic == topicId)
                                _appLogger.AddLog(LogProcess.Debug, topicId, $"Sync Page {page.PageId}", "Start uploading messages");

                            var result = await _messagesContentPersistentStorage.SyncAsync(topicId, page.PageId);
                            
                            
                            if (_appGlobalFlags.DebugTopic == topicId)
                                _appLogger.AddLog(LogProcess.Debug, topicId, $"Sync Page {page.PageId}", $"Upload result is {result}. Messages after upload {page.NotSavedAmount}");


                            while (result != SyncResult.Done)
                            {
                                _appLogger.AddLog(LogProcess.PagesLoaderOrGc, topicId, "PageId: " + page.PageId.Value,
                                    "There are messages to upload but no writer found. Creating one...");
                                await _messagesContentPersistentStorage.CreateNewPageAsync(topicId, page.PageId, ()=>writableContentCachePage);
                                result = await _messagesContentPersistentStorage.SyncAsync(topicId, page.PageId);
                            }
                        });
                    }
                    else
                    {
                        if (_appGlobalFlags.DebugTopic == topicId)
                            _appLogger.AddLog(LogProcess.Debug, topicId, $"Sync Page {page.PageId}", $"Skipping Page {page.GetType()}. Hash: {page.GetHashCode()}");
                    }
                }
            }

        }
        
    }
}