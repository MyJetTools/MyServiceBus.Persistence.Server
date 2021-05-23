using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.PersistenceOperations;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs
{
    public class ActivePagesWarmerAndGc
    {
        private readonly MessagesContentCache _messagesContentCache;
        private readonly MessagesContentReader _messagesContentReader;
        private readonly IAppLogger _logger;
        private readonly IMessagesContentPersistentStorage _messagesContentPersistentStorage;
        private readonly ActivePagesCalculator _activePagesCalculator;
        private readonly TaskSchedulerByTopic _taskSchedulerByTopic;
        private readonly AppGlobalFlags _appGlobalFlags;
        private readonly CompressPageBlobOperation _compressPageBlobOperation;

        public ActivePagesWarmerAndGc(MessagesContentCache messagesContentCache,
            MessagesContentReader messagesContentReader, IAppLogger logger, IMessagesContentPersistentStorage messagesContentPersistentStorage,
            ActivePagesCalculator activePagesCalculator, TaskSchedulerByTopic taskSchedulerByTopic,
            AppGlobalFlags appGlobalFlags, CompressPageBlobOperation compressPageBlobOperation)
        {
            _messagesContentCache = messagesContentCache;
            _messagesContentReader = messagesContentReader;
            _logger = logger;
            _messagesContentPersistentStorage = messagesContentPersistentStorage;
            _activePagesCalculator = activePagesCalculator;
            _taskSchedulerByTopic = taskSchedulerByTopic;
            _appGlobalFlags = appGlobalFlags;
            _compressPageBlobOperation = compressPageBlobOperation;
        }
        
        private static readonly TimeSpan GcTimeout = TimeSpan.FromSeconds(30);


        public async ValueTask CheckAndWarmItUpOrGcAsync()
        {

            if (!_appGlobalFlags.Initialized)
                return;

            var pages = _activePagesCalculator.GetActivePages();

            List<Task> tasks = null;

            foreach (var activePage in pages.Values)
            {
                if (_appGlobalFlags.IsShuttingDown)
                    return;

                tasks = WarmUpPages(activePage, tasks);

                tasks = GcPages(activePage, tasks);
            }

            if (tasks != null)
                await Task.WhenAll(tasks);

        }


        private List<Task> WarmUpPages(ActivePagesByTopic activePage, List<Task> tasks)
        {
            foreach (var pageId in activePage.Pages)
            {
                if (_messagesContentCache.HasPage(activePage.Snapshot.TopicId, pageId))
                    continue;
                    
                var task = _taskSchedulerByTopic.ExecuteTaskAsync(activePage.Snapshot.TopicId, pageId, "Warming Up",
                    () => WarmUpThreadTopicSynchronizedAsync(activePage.Snapshot.TopicId, pageId));

                tasks ??= new List<Task>();
                tasks.Add(task);
            }

            return tasks;
        }

        private List<Task> GcPages(ActivePagesByTopic activePage, List<Task> tasks)
        {
            var now = DateTime.UtcNow;
            //Garbage collect pages
            foreach (var loadedPage in _messagesContentCache.GetLoadedPages(activePage.Snapshot.TopicId))
            {
                    
                if (activePage.Pages.Any(activePageId => loadedPage.PageId.Value == activePageId.Value))
                    continue;

                if (now - loadedPage.LastAccessTime < GcTimeout)
                    continue;
                    
                var task = _taskSchedulerByTopic.ExecuteTaskAsync(activePage.Snapshot.TopicId, loadedPage.PageId, "GC page",
                    () => GcThreadTopicSynchronizedAsync(activePage.Snapshot.TopicId, loadedPage.PageId));

                tasks ??= new List<Task>();
                tasks.Add(task);
            }


            return tasks;
        }


        private async Task WarmUpThreadTopicSynchronizedAsync(string topicId, MessagePageId pageId)
        {
            _logger.AddLog(LogProcess.PagesLoaderOrGc, topicId,  "PageNo:"+pageId, "Warming up the page");
            await _messagesContentReader.LoadPageIntoCacheTopicSynchronizedAsync(topicId, pageId);
        }

        private async Task GcThreadTopicSynchronizedAsync(string topicId, MessagePageId pageId)
        {

            var page = _messagesContentCache.TryGetPage(topicId, pageId);

            if (page == null)
                return;
            
            var gcResult = await _messagesContentPersistentStorage.TryToGcAsync(topicId, pageId);

            if (gcResult.NotFound)
            {
                _logger.AddLog(LogProcess.PagesLoaderOrGc, topicId, "PageNo:"+pageId, "Attempt to GC PageWriter which is not found. Disposing page from the Cache") ;
                _messagesContentCache.DisposePage(topicId, pageId);
                return;
            }


            while (gcResult.NotReadyToGc)
            {
                _logger.AddLog(LogProcess.PagesLoaderOrGc, topicId, "PageNo:"+pageId, "Attempt to GC PageWriter which has not synced messages. Trying to Sync them to the Blob") ;
                await _messagesContentPersistentStorage.SyncAsync(topicId, pageId);
                gcResult = await _messagesContentPersistentStorage.TryToGcAsync(topicId, pageId);
            }

            if (gcResult.DisposedPageWriter != null)
            {
                            
                _logger.AddLog(LogProcess.PagesLoaderOrGc, topicId, "PageNo:"+pageId, "PageWriter disposed Ok.. Disposing From Cache");

                await _compressPageBlobOperation.ExecuteOperationThreadTopicSynchronizedAsync(topicId, pageId, gcResult.DisposedPageWriter.AssignedPage);
                _messagesContentCache.DisposePage(topicId, pageId);
                
                _logger.AddLog(LogProcess.PagesLoaderOrGc, topicId, "PageNo:"+pageId, "Page is disposed from Cache");
            }
        }


    }
}