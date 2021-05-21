using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations
{
    public class RestorePagePersistentOperation : PersistentOperationBase
    {
        public RestorePagePersistentOperation(string topicId, MessagePageId pageId, string reason) 
            : base(topicId, pageId, reason)
        {
        }

        public override void Inject(IServiceProvider serviceProvider)
        {
            _messagesContentPersistentStorage = serviceProvider.GetRequiredService<IMessagesContentPersistentStorage>();
            _messagesContentCache = serviceProvider.GetRequiredService<MessagesContentCache>();
            _appLogger = serviceProvider.GetRequiredService<IAppLogger>();
            _globalFlags = serviceProvider.GetRequiredService<AppGlobalFlags>();
            _compressedMessagesStorage = serviceProvider.GetRequiredService<ICompressedMessagesStorage>();
        }

        private IMessagesContentPersistentStorage _messagesContentPersistentStorage;

        private ICompressedMessagesStorage _compressedMessagesStorage;

        private MessagesContentCache _messagesContentCache;

        private IAppLogger _appLogger;

        private AppGlobalFlags _globalFlags;
        
        private IMessageContentPage RestoreCompressedPage( CompressedPage pageCompressedContent)
        {
            var restoredPage = new ReadOnlyContentPage(PageId, pageCompressedContent);
            return _messagesContentCache.AddPage(TopicId, restoredPage);
        }


        private string LogContext => "Page: " + PageId + "; Reason:" + Reason;
        
        private async Task<IMessageContentPage> TryRestoreCompressedPage()
        {
            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, TopicId, LogContext, $"Restoring page #{PageId} from compressed source");

            var pageCompressedContent = await _compressedMessagesStorage.GetCompressedPageAsync(TopicId, PageId);

            var dt = DateTime.UtcNow;

            if (pageCompressedContent.ZippedContent.Length == 0)
            {
                _appLogger.AddLog(LogProcess.PagesLoaderOrGc, TopicId, LogContext, 
                    $"Can not restore page #{PageId} from compressed source. Duration: {DateTime.UtcNow - dt}");
                
    
                return null;
            }

            var msgs = pageCompressedContent.Messages;
            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, TopicId, LogContext, 
                $"Restored page #{PageId} from compressed source. Duration: {DateTime.UtcNow - dt}. Messages: {msgs.Count}");
            
            return pageCompressedContent.ZippedContent.Length == 0 
                ? null 
                : RestoreCompressedPage(pageCompressedContent);
        }
        
        private async Task<IMessageContentPage> TryRestoreUncompressedPage()
        {
            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, TopicId, LogContext, $"Restoring page #{PageId} from UnCompressed source");

            var dt = DateTime.UtcNow;
            var pageWriter = await _messagesContentPersistentStorage.TryGetPageWriterAsync(TopicId, PageId);


            var messagesCount = 0;
            if (pageWriter == null)
            {
                var page = _messagesContentCache.GetOrCreateWritablePage(TopicId, PageId);
                pageWriter = await _messagesContentPersistentStorage.CreatePageWriterAsync(TopicId, PageId, false, page, _globalFlags);

                messagesCount = page.Count;
            }

            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, TopicId,LogContext, 
                $"Restored page #{PageId} from UnCompressed source. Duration: {DateTime.UtcNow - dt}. Messages: "+messagesCount);
            
            return pageWriter?.GetAssignedPage();
        }

        private async Task<IMessageContentPage> TryReadPageAsync()
        {
            var page = await TryRestoreCompressedPage();

            if (page != null)
                return page;

            return await TryRestoreUncompressedPage();
        }
        
        protected override async Task<IMessageContentPage> ExecuteOperationAsync()
        {
            var page = _messagesContentCache.TryGetPage(TopicId, PageId);

            if (page != null)
                return page;

            return await TryReadPageAsync();
        }

        public override string OperationFriendlyName => "Restoring page";
    }
}