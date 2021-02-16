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

        private readonly PageWritersCache _pageWritersCache = new PageWritersCache();

        public MessagesPersistentStorage(Func<(string topicId, MessagePageId pageId),IAzurePageBlob> getMessagesBlob)
        {
            _getMessagesBlob = getMessagesBlob;
        }
        
        /*
        public async Task WriteCompressedPageAsync(string topicId, MessagePageId pageId, ReadOnlyMemory<byte> pageData)
        {
            try
            {
                var azureBlob = _getMessagesBlob((topicId, pageId, true));
                await azureBlob.CreateIfNotExists();

                var dataLength = pageData.Length + sizeof(int);

                var blobCapacity = (int)MyAzurePageBlobUtils.CalculateRequiredBlobSize(0, dataLength, 1);
                
                await using var blobStream = new MemoryStream(blobCapacity);
            
                blobStream.WriteInt(pageData.Length);
                blobStream.Write(pageData.Span);
                    
                var theBlobStream = blobStream.GetDataReadyForPageBlob();
                theBlobStream.Position = 0;
            
                await azureBlob.WriteAsync(blobStream, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
      
        }
*/

        public async Task DeleteNonCompressedPageAsync(string topicId, MessagePageId pageId)
        {
            var azureBlob = _getMessagesBlob((topicId, pageId));

            if (await azureBlob.ExistsAsync())
                await azureBlob.DeleteAsync();
        }

        /*
        public async Task<ReadOnlyMemory<byte>> GetCompressedPageAsync(string topicId, MessagePageId pageId)
        {
            var azureBlob = _getMessagesBlob((topicId, pageId, true));

            if (!await azureBlob.ExistsAsync())
                return new ReadOnlyMemory<byte>();
            
            var stream = await azureBlob.DownloadAsync();

            var dataSize = stream.ReadInt();

            var buffer = stream.GetBuffer();

            return new ReadOnlyMemory<byte>(buffer, sizeof(int), dataSize);
        }

        
        public async Task<bool> HasCompressedPageAsync(string topicId, MessagePageId pageId)
        {
            var azureBlob = _getMessagesBlob((topicId, pageId, true));
            return await azureBlob.ExistsAsync();
        }
*/

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
            return new ValueTask<IPageWriter>(_pageWritersCache.TryGet(topicId, pageId));
        }

    }
}