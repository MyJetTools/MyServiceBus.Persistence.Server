using System;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.AzureStorage.TopicMessages
{


    public class MessagesPersistentStorage : IMessagesContentPersistentStorage
    {

        private readonly Func<(string topicId, MessagePageId pageId), IAzurePageBlob> _getMessagesBlob;

        private readonly PageWritersCache _pageWritersCache = new ();

        public MessagesPersistentStorage(Func<(string topicId, MessagePageId pageId),IAzurePageBlob> getMessagesBlob)
        {
            _getMessagesBlob = getMessagesBlob;
        }

        public async Task DeleteNonCompressedPageAsync(string topicId, MessagePageId pageId)
        {
            var azureBlob = _getMessagesBlob((topicId, pageId));

            if (await azureBlob.ExistsAsync())
                await azureBlob.DeleteAsync();
        }
        
        public async Task<IPageWriter> CreatePageWriterAsync(string topicId, MessagePageId pageId,
            bool cleanIt, WritableContentCachePage page, AppGlobalFlags appGlobalFlags)
        {
            var messagesBlob = _getMessagesBlob((topicId, pageId));
            

            if (cleanIt)
            {
                if (await messagesBlob.ExistsAsync())
                {
                    var blobSize = await messagesBlob.GetBlobSizeAsync();
                    if (blobSize > 0)
                        await messagesBlob.ResizeBlobAsync(0); 
                }
            }

            var result = new PageWriter(topicId, pageId, messagesBlob, appGlobalFlags.LoadBlobPagesSize);

            await result.AssignPageAndInitialize(page, appGlobalFlags);

            return result;
        }
        

        public ValueTask<IPageWriter> TryGetPageWriterAsync(string topicId, MessagePageId pageId)
        {
            return new (_pageWritersCache.TryGet(topicId, pageId));
        }

    }
}