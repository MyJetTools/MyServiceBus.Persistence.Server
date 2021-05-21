using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations;
using MyServiceBus.Persistence.Domains.IndexByMinute;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs
{
    public class PagesToCompressDetector
    {
        private readonly PersistentOperationsScheduler _scheduler;
        private readonly IndexByMinuteWriter _indexByMinuteWriter;
        private readonly IAppLogger _appLogger;
        private readonly QueueSnapshotCache _queueSnapshotCache;
        private readonly AppGlobalFlags _appGlobalFlags;
        private readonly ILastCompressedPageStorage _lastCompressedPageStorage;
        private readonly MessagesContentReader _messagesContentReader;
        private readonly ICompressedMessagesStorage _compressedMessagesStorage;

        public PagesToCompressDetector(PersistentOperationsScheduler scheduler, IndexByMinuteWriter indexByMinuteWriter,
            IAppLogger appLogger, QueueSnapshotCache queueSnapshotCache, AppGlobalFlags appGlobalFlags,
            ILastCompressedPageStorage lastCompressedPageStorage,
            MessagesContentReader messagesContentReader, ICompressedMessagesStorage compressedMessagesStorage)
        {
            _scheduler = scheduler;
            _indexByMinuteWriter = indexByMinuteWriter;
            _appLogger = appLogger;
            _queueSnapshotCache = queueSnapshotCache;
            _appGlobalFlags = appGlobalFlags;
            _lastCompressedPageStorage = lastCompressedPageStorage;
            _messagesContentReader = messagesContentReader;
            _compressedMessagesStorage = compressedMessagesStorage;
        }


        public async IAsyncEnumerable<(string topicId, IMessageContentPage page)> GetPageToCompressAsync()
        {
            var (_, topicsAndQueues) = _queueSnapshotCache.Get();

            foreach (var topicAndQueues in topicsAndQueues)
            {

                var currentPageId = MessagesContentPagesUtils.GetPageId(topicAndQueues.MessageId);

                var lastCompressedPage =
                    await _lastCompressedPageStorage.GetLastCompressedPageAsync(topicAndQueues.TopicId);
                var pageToCompress = lastCompressedPage.NextPage();

                if (pageToCompress.Value >= currentPageId.Value)
                    continue;


                var hasCompressedPage =
                    await _compressedMessagesStorage.HasCompressedPageAsync(topicAndQueues.TopicId, pageToCompress);

                if (hasCompressedPage)
                {
                    _appLogger.AddLog(LogProcess.PagesCompressor, topicAndQueues.TopicId,
                        $"Page {pageToCompress.Value} is already compressed. Skipping");
                    await _lastCompressedPageStorage.SaveLastCompressedPageStorageAsync(topicAndQueues.TopicId,
                        pageToCompress);
                    continue;
                }


                var page = await _messagesContentReader.TryGetPageAsync(topicAndQueues.TopicId, pageToCompress,
                    "Compressor Detector");

                if (pageToCompress.Value == currentPageId.Value - 1)
                {
                    if (page.HasAllMessages())
                        yield return (topicAndQueues.TopicId, page);
                }
                else
                {
                    yield return (topicAndQueues.TopicId, page);
                }

            }


        }


        public async ValueTask TimerAsync()
        {

            if (!_appGlobalFlags.Initialized)
                return;


            await foreach (var (topicId, page) in GetPageToCompressAsync())
            {
                if (_appGlobalFlags.IsShuttingDown)
                    return;

                _appLogger.AddLog(LogProcess.PagesCompressor, topicId, $"Detected the page {page.PageId.Value} which has to be compressed");
                await _scheduler.CompressPageAsync(topicId, page, "Compressor Detector");
                
                await _lastCompressedPageStorage.SaveLastCompressedPageStorageAsync(topicId,
                    page.PageId);
                
                _appLogger.AddLog(LogProcess.PagesCompressor, topicId,
                    $"Page {page.Count} compressed and saved");

                _indexByMinuteWriter.NewMessages(topicId, page.GetMessages());
            }
        }



    }

}