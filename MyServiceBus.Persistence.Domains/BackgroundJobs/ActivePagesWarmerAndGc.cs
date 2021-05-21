using System;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs
{
    public class ActivePagesWarmerAndGc
    {
        private readonly QueueSnapshotCache _queueSnapshotCache;
        private readonly MessagesContentCache _messagesContentCache;
        private readonly MessagesContentReader _messagesContentReader;
        private readonly IAppLogger _logger;
        private readonly IMessagesContentPersistentStorage _messagesContentPersistentStorage;
        private readonly AppGlobalFlags _appGlobalFlags;
        
        
        public readonly TimeSpan GcDelay = TimeSpan.FromSeconds(30);

        public ActivePagesWarmerAndGc(QueueSnapshotCache queueSnapshotCache, MessagesContentCache messagesContentCache,
            MessagesContentReader messagesContentReader, IAppLogger logger, IMessagesContentPersistentStorage messagesContentPersistentStorage,
            AppGlobalFlags appGlobalFlags)
        {
            _queueSnapshotCache = queueSnapshotCache;
            _messagesContentCache = messagesContentCache;
            _messagesContentReader = messagesContentReader;
            _logger = logger;
            _messagesContentPersistentStorage = messagesContentPersistentStorage;
            _appGlobalFlags = appGlobalFlags;
        }


        public async ValueTask CheckAndWarmItUpAsync()
        {
            
            if (!_appGlobalFlags.Initialized)
                return;
            


            var (_, topicsAndQueues) = _queueSnapshotCache.Get();

            foreach (var topicSnapshot in topicsAndQueues)
            {
                if (_appGlobalFlags.IsShuttingDown)
                    return;
                
                var activePages = topicSnapshot.GetActivePages();

                var pagesInCache = _messagesContentCache.GetLoadedPages(topicSnapshot.TopicId);

                foreach (var pageIdToWarmUp in pagesInCache.GetPagesToWarmUp(activePages))
                {
                    _logger.AddLog(LogProcess.PagesLoaderOrGc, topicSnapshot.TopicId, "Warming up the page", "PageNo:"+pageIdToWarmUp);
                    
                    await _messagesContentReader.TryGetPageAsync(topicSnapshot.TopicId, pageIdToWarmUp, "Warm Up");
                }

                foreach (var pageToGc in pagesInCache.GetPagesToGarbageCollect(activePages))
                {
                    var page = _messagesContentCache.TryGetPage(topicSnapshot.TopicId, pageToGc);
                    
                    if (page == null)
                        continue;
                    
                    if (DateTime.UtcNow  - page.LastAccessTime < GcDelay)
                        continue;

                    await _messagesContentPersistentStorage.GcAsync(topicSnapshot.TopicId, pageToGc);

                    _messagesContentCache.DisposePage(topicSnapshot.TopicId, pageToGc);
                }
                
            }
        }


    }
}