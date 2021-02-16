using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;

namespace MyServiceBus.Persistence.AzureStorage.CompressedMessages
{

    public struct LastPageBlob
    {
        public string TopicId { get; set; }
        public MessagePageId MessagePageId { get; set; }
        public IAzurePageBlob AzurePageBlob { get; set; }
        
    }
    
    public class LegacyCompressedMessagesStorage : ILegacyCompressedMessagesStorage
    {
        private readonly Func<(string topicId, MessagePageId pageId), IAzurePageBlob> _getAzurePageBlob;


        private LastPageBlob _lastPageBlob;

        public LegacyCompressedMessagesStorage(Func<(string topicId, MessagePageId pageId), IAzurePageBlob> getAzurePageBlob)
        {
            _getAzurePageBlob = getAzurePageBlob;
        }
        
        private IAzurePageBlob GetAzurePageBlob(string topicId, MessagePageId pageId)
        {
            if (_lastPageBlob.TopicId == topicId && _lastPageBlob.MessagePageId.EqualsWith(pageId))
                return _lastPageBlob.AzurePageBlob;

            var result = _getAzurePageBlob((topicId, pageId));
            _lastPageBlob.TopicId = topicId;
            _lastPageBlob.MessagePageId = pageId;
            _lastPageBlob.AzurePageBlob = result;

            return result;
        }

        private async Task<ReadOnlyMemory<byte>> DownloadCompressedPageAsync(string topicId, MessagePageId pageId)
        {
            var azureBlob = GetAzurePageBlob(topicId, pageId);

            if (!await azureBlob.ExistsAsync())
                return new ReadOnlyMemory<byte>();
            
            var stream = await azureBlob.DownloadAsync();

            var dataSize = stream.ReadInt();

            var buffer = stream.GetBuffer();

            return new ReadOnlyMemory<byte>(buffer, sizeof(int), dataSize);
        }

        public async Task<CompressedPage> GetCompressedPageAsync(string topicId, MessagePageId pageId)
        {
            var result = await DownloadCompressedPageAsync(topicId, pageId);
            return new CompressedPage(result);
        }

        public async ValueTask<bool> HasCompressedPageAsync(string topicId, MessagePageId pageId)
        {
            var azureBlob = GetAzurePageBlob(topicId, pageId);
            return await azureBlob.ExistsAsync();
        }

        public async ValueTask DeleteIfExistsAsync(string topicId, MessagePageId pageId)
        {
            var azureBlob = GetAzurePageBlob(topicId, pageId);

            if (await azureBlob.ExistsAsync())
                await azureBlob.DeleteAsync();
        }
    }
}