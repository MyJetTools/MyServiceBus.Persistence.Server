using System.Linq;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.ExecutionProgress;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;

namespace MyServiceBus.Persistence.Domains.MessagesContentCompressed
{

    public enum PageCompressionResult
    {
        Ok, TopicNotFound, PageCanNotBeCompressedSinceItsActive, PageIsAlreadyCompressed, PageDoesNotExists
    }
    
    public class ContentCompressorProcessor
    {
        private readonly QueueSnapshotCache _queueSnapshotCache;
        private readonly ICompressedMessagesStorage _compressedMessagesStorage;
        private readonly MessagesContentReader _messagesContentReader;
        private readonly IAppLogger _appLogger;
        private readonly TopicsList _topicsList;
        private readonly IMessagesContentPersistentStorage _persistentStorage;


        public ContentCompressorProcessor(QueueSnapshotCache queueSnapshotCache,
            ICompressedMessagesStorage compressedMessagesStorage, 
            MessagesContentReader messagesContentReader,
            IAppLogger appLogger,
            TopicsList topicsList,
            IMessagesContentPersistentStorage persistentStorage)
        {
            _queueSnapshotCache = queueSnapshotCache;
            _compressedMessagesStorage = compressedMessagesStorage;
            _messagesContentReader = messagesContentReader;
            _appLogger = appLogger;
            _topicsList = topicsList;
            _persistentStorage = persistentStorage;
        }
        
        public bool IsPageActive(string topicId, MessagePageId pageId)
        {
            var snapshot = _queueSnapshotCache.Get();

            var topicSnapshot = snapshot.Cache.FirstOrDefault(itm => itm.TopicId == topicId);

            if (topicSnapshot == null)
                return false;

            var activePageId = MessagesContentPagesUtils.GetPageId(topicSnapshot.MessageId);

            return pageId.Value>=activePageId.Value - 1;
        }



        private async ValueTask CompressPageUnderLockAsync(TopicDataLocator topicDataLocator, IMessageContentPage page,
            RequestHandler requestHandler)
        {
            requestHandler.CurrentProcess = "Compressing the page";
            
            _appLogger.AddLog(topicDataLocator.TopicId, $"Verifying compressed data for page {page.PageId}");
            var compressedPageToVerify = await _compressedMessagesStorage.GetCompressedPageAsync(topicDataLocator.TopicId, page.PageId);
            
            var messages = compressedPageToVerify.UnCompress();

            _appLogger.AddLog(topicDataLocator.TopicId, $"Verified compressed data for page {page.PageId}. Messages: " + messages.Count);
            
            _appLogger.AddLog(topicDataLocator.TopicId, $"Deleting Uncompressed data for page {page.PageId}");
            await _persistentStorage.DeleteNonCompressedPageAsync(topicDataLocator.TopicId, page.PageId);
            
            _appLogger.AddLog(topicDataLocator.TopicId, "Written Compressed Page: " + page.PageId +". Messages in the page:"+messages.Count);

        }

        private async ValueTask<PageCompressionResult> CompressPageUnderLockAsync(TopicDataLocator topicDataLocator,
            MessagePageId pageId, RequestHandler requestHandler)
        {

            requestHandler.CurrentProcess = "Loading page to compress";
            
            if (IsPageActive(topicDataLocator.TopicId, pageId))
                return PageCompressionResult.PageCanNotBeCompressedSinceItsActive;
            
            
            if (await _compressedMessagesStorage.HasCompressedPageAsync(topicDataLocator.TopicId, pageId))
                return PageCompressionResult.PageIsAlreadyCompressed;

            var page = await _messagesContentReader.RestorePageUnderLockAsync(topicDataLocator, pageId);
            
            if (page == null)
                return PageCompressionResult.PageDoesNotExists;

            await CompressPageAsync(topicDataLocator, page, requestHandler);

            return PageCompressionResult.Ok;
        }


        public async ValueTask<PageCompressionResult> CompressPageAsync(string topicId, 
            MessagePageId pageId, RequestHandler requestHandler)
        {
            var topicDataLocator = _topicsList.TryGet(topicId);
            if (topicDataLocator == null)
                return PageCompressionResult.TopicNotFound;

            using (await topicDataLocator.AsyncLock.LockAsync(requestHandler))
            {
                return await CompressPageUnderLockAsync(topicDataLocator, pageId, requestHandler);
            }
        }
        
        public async ValueTask CompressPageAsync(TopicDataLocator topicDataLocator, 
            IMessageContentPage page, RequestHandler requestHandler)
        {
            using (await topicDataLocator.AsyncLock.LockAsync(requestHandler))
            {
                await CompressPageUnderLockAsync(topicDataLocator, page, requestHandler);
            }
        }
    }
}