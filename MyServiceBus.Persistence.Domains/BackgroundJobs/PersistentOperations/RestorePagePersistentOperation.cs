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
        
        private async Task<IMessageContentPage> TryRestoreCompressedPage()
        {
            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, TopicId, $"Restoring page #{PageId} from compressed source");

            var pageCompressedContent = await _compressedMessagesStorage.GetCompressedPageAsync(TopicId, PageId);

            var dt = DateTime.UtcNow;

            if (pageCompressedContent.Content.Length == 0)
            {
                _appLogger.AddLog(LogProcess.PagesLoaderOrGc, TopicId,
                    $"Can not restore page #{PageId} from compressed source. Duration: {DateTime.UtcNow - dt}");
                return null;
            }
            
            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, TopicId,
                $"Restored page #{PageId} from compressed source. Duration: {DateTime.UtcNow - dt}");
            
            return pageCompressedContent.Content.Length == 0 
                ? null 
                : RestoreCompressedPage(pageCompressedContent);
        }
        
        private async Task<IMessageContentPage> TryRestoreUncompressedPage()
        {
            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, TopicId, $"Restoring page #{PageId} from UnCompressed source");

            var dt = DateTime.UtcNow;
            var pageWriter = await _messagesContentPersistentStorage.TryGetPageWriterAsync(TopicId, PageId);

            if (pageWriter == null)
            {
                var page = _messagesContentCache.GetOrCreateWritablePage(TopicId, PageId);
                pageWriter = await _messagesContentPersistentStorage.CreatePageWriterAsync(TopicId, PageId, false, page, _globalFlags);
            }

            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, TopicId,
                $"Restored page #{PageId} from UnCompressed source. Duration: {DateTime.UtcNow - dt}");
            
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