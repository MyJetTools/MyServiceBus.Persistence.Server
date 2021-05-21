using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;

namespace MyServiceBus.Persistence.Domains.PersistenceOperations
{
    public class CompressPageBlobOperation
    {
        private readonly IMessagesContentPersistentStorage _persistentStorage;

        private readonly ICompressedMessagesStorage _compressedMessagesStorage;

        private readonly IAppLogger _appLogger;


        public CompressPageBlobOperation(IMessagesContentPersistentStorage persistentStorage, ICompressedMessagesStorage compressedMessagesStorage,
            IAppLogger appLogger)
        {
            _persistentStorage = persistentStorage;
            _compressedMessagesStorage = compressedMessagesStorage;
            _appLogger = appLogger;
        }
    

        public async Task ExecuteOperationAsync(string topicId, MessagePageId pageId, IMessageContentPage messageContentPage)
        {
            var logContext = "Page: " + pageId.Value;
            if (await _compressedMessagesStorage.HasCompressedPageAsync(topicId, pageId))
            {
                _appLogger.AddLog(LogProcess.PagesCompressor,   topicId,  logContext, "Has compressed page. Skipping compressing procedure");
                return;
            }
   

            if (messageContentPage.Count == 0)
            {
                _appLogger.AddLog(LogProcess.PagesCompressor,   topicId,  logContext, "No messages to compress. Skipping compressing procedure");
                return;
            }
            
            var compressedPage = messageContentPage.GetCompressedPage();
            
            _appLogger.AddLog(LogProcess.PagesCompressor,   topicId,  logContext, $"Writing Compressed data for page {pageId}.");
            await _compressedMessagesStorage.WriteCompressedPageAsync(topicId, pageId, compressedPage, _appLogger);
            
            _appLogger.AddLog(LogProcess.PagesCompressor, topicId, logContext, $"Verifying compressed data for page {pageId}");
            var compressedPageToVerify = await _compressedMessagesStorage.GetCompressedPageAsync(topicId, pageId);

            var messages = compressedPageToVerify.Messages;

            _appLogger.AddLog(LogProcess.PagesCompressor, topicId, logContext, $"Verified compressed data for page {pageId}. Messages: " + messages.Count);
            
            _appLogger.AddLog(LogProcess.PagesCompressor, topicId, logContext, $"Deleting Uncompressed page {pageId}");
            await _persistentStorage.DeleteNonCompressedPageAsync(topicId, pageId);

            _appLogger.AddLog(LogProcess.PagesCompressor, topicId, logContext, "Written Compressed Page: " + pageId +". Messages in the page:"+messageContentPage.Count);

        }

    }
}