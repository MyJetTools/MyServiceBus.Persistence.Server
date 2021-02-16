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
        private readonly AppGlobalFlags _appGlobalFlags;
        
        
        public readonly TimeSpan GcDelay = TimeSpan.FromSeconds(30);

        public ActivePagesWarmerAndGc(QueueSnapshotCache queueSnapshotCache, MessagesContentCache messagesContentCache,
            MessagesContentReader messagesContentReader, IAppLogger logger, 
            AppGlobalFlags appGlobalFlags)
        {
            _queueSnapshotCache = queueSnapshotCache;
            _messagesContentCache = messagesContentCache;
            _messagesContentReader = messagesContentReader;
            _logger = logger;
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
                    _logger.AddLog(topicSnapshot.TopicId, "Warming up the page "+pageIdToWarmUp);
                    
                    await _messagesContentReader.TryGetPageAsync(topicSnapshot.TopicId, pageIdToWarmUp, "Warm Up");
                }

                foreach (var pageToGc in pagesInCache.GetPagesToGarbageCollect(activePages))
                {
                    var page = _messagesContentCache.TryGetPage(topicSnapshot.TopicId, pageToGc);
                    
                    if (page == null)
                        continue;
                    
                    if (DateTime.UtcNow  - page.LastAccessTime < GcDelay)
                        continue;

                    _messagesContentCache.DisposePage(topicSnapshot.TopicId, pageToGc);
                }
                
            }
        }


    }
}