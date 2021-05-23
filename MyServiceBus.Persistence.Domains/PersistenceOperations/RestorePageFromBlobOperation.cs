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
            var logContext = "PageId: " + pageId.Value;
     
                var resultFromCache = _messagesContentCache.TryGetPage(topicId, pageId);

                if (resultFromCache != null)
                    return resultFromCache;


                _appLogger.AddLog(LogProcess.PagesLoaderOrGc, topicId, logContext,
                    $"Restoring page #{pageId} from UnCompressed source");

                var dt = DateTime.UtcNow;

                var pageWriter = await _messagesContentPersistentStorage.TryGetAsync(topicId, pageId, 
                    () =>
                    {
                        var result = _messagesContentCache.CreateWritablePage(topicId, pageId);

                        if (result.Result != null)
                            return result.Result;


                        if (result.Exists != null)
                        {
                            if (result.Exists is WritableContentPage existingWritablePage)
                            {
                                _appLogger.AddLog(LogProcess.PagesLoaderOrGc, topicId, logContext, "Restoring uncompressed page and found existing writable content page. Using it");
                                return existingWritablePage;
                            }

                            throw new Exception(
                                $"Trying to create page {topicId}/{pageId} by found non writable content page {result.Exists.GetType()}");

                        }

                        throw new Exception($"RestorePageFromBlobOperation.TryRestoreFromUncompressedPage  I should not be here. {topicId}/{pageId}");


                    });

                if (pageWriter == null)
                {
                    _appLogger.AddLog(LogProcess.PagesLoaderOrGc, topicId, logContext,
                        "Can not restore page from uncompressed page");
                    return null;
                }


                _appLogger.AddLog(LogProcess.PagesLoaderOrGc, topicId, logContext,
                    $"Restored page #{pageId} from UnCompressed source. Duration: {DateTime.UtcNow - dt}. Messages: " +
                    pageWriter.AssignedPage.Count);

                return pageWriter.AssignedPage;
        }
     
    }
}