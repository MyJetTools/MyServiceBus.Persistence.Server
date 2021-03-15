using System;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.ExecutionProgress;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs
{
    public class LoadedPagesGcBackgroundProcessor
    {
        private readonly QueueSnapshotCache _queueSnapshotCache;
        private readonly TopicsList _topicsList;
        private readonly IAppLogger _logger;
        private readonly ContentCompressorProcessor _compressorProcessor;
        private readonly CurrentRequests _currentRequests;
        private readonly AppGlobalFlags _appGlobalFlags;
        
        
        public readonly TimeSpan GcDelay = TimeSpan.FromSeconds(30);

        public LoadedPagesGcBackgroundProcessor(QueueSnapshotCache queueSnapshotCache, TopicsList topicsList,
            IAppLogger logger, 
            ContentCompressorProcessor compressorProcessor, 
            CurrentRequests currentRequests,
            AppGlobalFlags appGlobalFlags)
        {
            _queueSnapshotCache = queueSnapshotCache;
            _topicsList = topicsList;
            _logger = logger;
            _compressorProcessor = compressorProcessor;
            _currentRequests = currentRequests;
            _appGlobalFlags = appGlobalFlags;
        }

        public async ValueTask CheckAndGcIfNeededAsync()
        {
            
            if (!_appGlobalFlags.Initialized)
                return;


            var (_, topicsSnapshot) = _queueSnapshotCache.Get();

            foreach (var topicAndQueuesSnapshot in topicsSnapshot)
            {
                if (_appGlobalFlags.IsShuttingDown)
                    return;

                var topicDataLocator = _topicsList.TryGet(topicAndQueuesSnapshot.TopicId);
                
                if (topicDataLocator == null)
                    continue;

                var loadedPages = topicDataLocator.GetLoadedPages();

                var activePage = topicAndQueuesSnapshot.GetActivePageId();

                foreach (var page in loadedPages)
                {
                    using var requestHandler = _currentRequests.StartRequest(topicDataLocator, "LoadedPagesGc");

                    if (page.PageId.Value < activePage.Value && !page.IsCompressed)
                        await _compressorProcessor.CompressPageAsync(topicDataLocator, page, requestHandler);

                    if (DateTime.UtcNow - page.LastAccessTime > GcDelay)
                    {
                        _logger.AddLog(topicDataLocator.TopicId, $"GCing page {page.PageId}");
                        topicDataLocator.RemovePage(page.PageId);
                    }
                }
            }
        }


    }
}