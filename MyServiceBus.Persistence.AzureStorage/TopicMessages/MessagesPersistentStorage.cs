using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.Metrics;

namespace MyServiceBus.Persistence.AzureStorage.TopicMessages
{


    public class MessagesPersistentStorage : IMessagesContentPersistentStorage
    {

        private readonly Func<(string topicId, MessagePageId pageId), IAzurePageBlob> _getMessagesBlob;
        private readonly AppGlobalFlags _appGlobalFlags;

        private readonly Dictionary<string, PageWritersCache> _pageWritersCacheByTopic = new ();

        private object _lockObject = new();


        private PageWritersCache GetOrCreatePageWritersCache(string topicId)
        {
            lock (_lockObject)
            {

                if (_pageWritersCacheByTopic.TryGetValue(topicId, out var result))
                    return result;

                result = new PageWritersCache(topicId, _getMessagesBlob, _appGlobalFlags);
                
                _pageWritersCacheByTopic.Add(topicId, result);

                return result;
            }
            
        }

        public MessagesPersistentStorage(Func<(string topicId, MessagePageId pageId),IAzurePageBlob> getMessagesBlob, AppGlobalFlags appGlobalFlags)
        {
            _getMessagesBlob = getMessagesBlob;
            _appGlobalFlags = appGlobalFlags;
        }

        public async Task DeleteNonCompressedPageAsync(string topicId, MessagePageId pageId)
        {
            var azureBlob = _getMessagesBlob((topicId, pageId));

            if (await azureBlob.ExistsAsync())
                await azureBlob.DeleteAsync();
        }
        

        public async ValueTask<IPageWriter> GetOrCreateAsync(string topicId, MessagePageId pageId)
        {
            var cache = GetOrCreatePageWritersCache(topicId);
            return await cache.GetOrCreateAsync(pageId);
        }

        public async ValueTask<IPageWriter> TryGetAsync(string topicId, MessagePageId pageId)
        {
            var cache = GetOrCreatePageWritersCache(topicId);
            return await cache.GetOrCreateAsync(pageId);
        }


        private IReadOnlyList<PageWriter> GetPageWriterReadyToSync(string topicId)
        {
            List<PageWriter> result = null;

            lock (_lockObject)
            {
                if (!_pageWritersCacheByTopic.ContainsKey(topicId))
                    return null;

                foreach (var pageWriter in _pageWritersCacheByTopic[topicId].GetPageWritersWeHaveToSync())
                {
                    result ??= new List<PageWriter>();
                    result.Add(pageWriter);
                }
            }

            return result;
        }


        public async Task<long> SyncAsync(string topicId)
        {
            var pagesToSync = GetPageWriterReadyToSync(topicId);
            
            if (pagesToSync == null)
                return -1;
            
            long result = -1;
            
            foreach (var pageWriter in pagesToSync)
            {
               await pageWriter.SyncIfNeededAsync();
               
               if (result<pageWriter.MaxMessageIdInBlob)
                    result = pageWriter.MaxMessageIdInBlob;
            }

            return result;
        }


        private PageWriter GcWriter(string topicId, MessagePageId pageId)
        {
            lock (_lockObject)
            {
                if (_pageWritersCacheByTopic.ContainsKey(topicId))
                    return _pageWritersCacheByTopic[topicId].Remove(pageId);
            }

            return null;
        }

        public async Task GcAsync(string topicId, MessagePageId pageId)
        {
            var writer = GcWriter(topicId, pageId);

            if (writer != null)
                await writer.DisposeAsync();
        }
    }
}