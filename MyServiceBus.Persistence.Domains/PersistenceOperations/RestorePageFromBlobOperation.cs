using System;
using System.Linq;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;

namespace MyServiceBus.Persistence.Domains.PersistenceOperations
{
    public class RestorePageFromBlobOperation 
    {
        private readonly IMessagesContentPersistentStorage _messagesContentPersistentStorage;

        private readonly ICompressedMessagesStorage _compressedMessagesStorage;

        private readonly MessagesContentCache _messagesContentCache;

        private readonly IAppLogger _appLogger;
        public RestorePageFromBlobOperation(IAppLogger appLogger, IMessagesContentPersistentStorage messagesContentPersistentStorage,
            ICompressedMessagesStorage compressedMessagesStorage, MessagesContentCache messagesContentCache)
        {
            _appLogger = appLogger;
            _messagesContentPersistentStorage = messagesContentPersistentStorage;
            _compressedMessagesStorage = compressedMessagesStorage;
            _messagesContentCache = messagesContentCache;
        }

        public async Task<IMessageContentPage> TryRestoreFromCompressedPage(string topicId, MessagePageId pageId)
        {

            var resultFromCache = _messagesContentCache.TryGetPage(topicId, pageId);

            if (resultFromCache != null)
                return resultFromCache;


            var logContext = "PageId: " + pageId.Value;

            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, topicId, logContext,
                $"Restoring page #{pageId} from compressed source");

            var pageCompressedContent = await _compressedMessagesStorage.GetCompressedPageAsync(topicId, pageId);

            var dt = DateTime.UtcNow;

            if (pageCompressedContent.ZippedContent.Length == 0)
            {
                _appLogger.AddLog(LogProcess.PagesLoaderOrGc, topicId, logContext,
                    $"Can not restore page #{pageId} from compressed source. Duration: {DateTime.UtcNow - dt}");
                return null;
            }

            var msgs = pageCompressedContent.Messages;
            long minId = 0;
            long maxId = 0;

            if (msgs.Count > 0)
            {
                minId = msgs.Min(itm => itm.MessageId);
                maxId = msgs.Max(itm => itm.MessageId);
            }

            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, topicId, logContext,
                $"Restored page #{pageId} from compressed source. Duration: {DateTime.UtcNow - dt}. Messages: {msgs.Count}. MinId: {minId}, MaxId: {maxId}");


            var result = new ReadOnlyContentPage(pageCompressedContent);
            _messagesContentCache.AddPage(topicId, result);
            return result;

        }

        public async Task<IMessageContentPage> TryRestoreFromUncompressedPage(string topicId, MessagePageId pageId)
        {
            
            var resultFromCache = _messagesContentCache.TryGetPage(topicId, pageId);

            if (resultFromCache != null)
                return resultFromCache;

            var logContext = "PageId: " + pageId.Value;
            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, topicId, logContext,
                $"Restoring page #{pageId} from UnCompressed source");

            var dt = DateTime.UtcNow;

            var pageWriter = await _messagesContentPersistentStorage.TryGetAsync(topicId, pageId);

            _messagesContentCache.AddPage(topicId, pageWriter.AssignedPage);

            _appLogger.AddLog(LogProcess.PagesLoaderOrGc, topicId, logContext,
                $"Restored page #{pageId} from UnCompressed source. Duration: {DateTime.UtcNow - dt}. Messages: " +
                pageWriter.AssignedPage.Count + $" MinId: {pageWriter.AssignedPage.MinMessageId}, MaxId: {pageWriter.AssignedPage.MaxMessageId}. " +
                $"MinMax Difference: {pageWriter.AssignedPage.MaxMessageId - pageWriter.AssignedPage.MinMessageId + 1}");

            return pageWriter.AssignedPage;
        }
     
    }
}