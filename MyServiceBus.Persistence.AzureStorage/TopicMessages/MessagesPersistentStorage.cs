using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Domains.Metrics;

namespace MyServiceBus.Persistence.AzureStorage.TopicMessages
{

    public class MessagesPersistentStorage : IMessagesContentPersistentStorage
    {

        private readonly Func<(string topicId, MessagePageId pageId), IAzurePageBlob> _getMessagesBlob;

        private readonly Dictionary<string, PageWritersCache> _pageWritersCacheByTopic = new ();

        private readonly object _lockObject = new();


        private MetricsByTopic _metricsByTopic;
        private AppGlobalFlags _appGlobalFlags;
        
        private PageWritersCache GetOrCreatePageWritersCache(string topicId)
        {
            lock (_lockObject)
            {

                if (_pageWritersCacheByTopic.TryGetValue(topicId, out var result))
                    return result;

                result = new PageWritersCache(topicId, _getMessagesBlob, _appGlobalFlags, _metricsByTopic.Get(topicId));
                
                _pageWritersCacheByTopic.Add(topicId, result);

                return result;
            }
            
        }

        public MessagesPersistentStorage(Func<(string topicId, MessagePageId pageId),IAzurePageBlob> getMessagesBlob)
        {
            _getMessagesBlob = getMessagesBlob;
   
        }

        public void Inject(IServiceProvider sp)
        {
            _metricsByTopic = sp.GetRequiredService<MetricsByTopic>();
            _appGlobalFlags = sp.GetRequiredService<AppGlobalFlags>();
        }

        public async Task DeleteNonCompressedPageAsync(string topicId, MessagePageId pageId)
        {
            var azureBlob = _getMessagesBlob((topicId, pageId));

            if (await azureBlob.ExistsAsync())
                await azureBlob.DeleteAsync();
        }
        

        public async ValueTask CreateNewPageAsync(string topicId, MessagePageId pageId, Func<WritableContentPage> getWritableContentCachePage)
        {
            var cacheByTopic = GetOrCreatePageWritersCache(topicId);
            await cacheByTopic.CreateNewOrLoadAsync(pageId, getWritableContentCachePage);
        }

        public async ValueTask<IPageWriter> TryGetAsync(string topicId, MessagePageId pageId, Func<WritableContentPage> getWritableContentCachePage)
        {
            var cache = GetOrCreatePageWritersCache(topicId);
            return await cache.TryGetAsync(pageId, getWritableContentCachePage);
        }

        private PageWriter TryGet(string topicId, MessagePageId pageId)
        {
            lock (_lockObject)
            {
                return _pageWritersCacheByTopic.TryGetValue(topicId, out var pageWriter) ? pageWriter.TryGetOrNull(pageId) : null;
            }
        }


        public async Task<SyncResult> SyncAsync(string topicId, MessagePageId pageId)
        {
            var pageToExecute = TryGet(topicId, pageId);

            if (pageToExecute == null)
                return SyncResult.WriterNotFound;

            await pageToExecute.SyncIfNeededAsync();

            return SyncResult.Done;
        }



        public async ValueTask<GcWriterResult> TryToGcAsync(string topicId, MessagePageId pageId)
        {
            PageWriter result;

            lock (_lockObject)
            {
                if (!_pageWritersCacheByTopic.ContainsKey(topicId))
                    return new GcWriterResult
                    {
                        NotFound = true
                    };

                result = _pageWritersCacheByTopic[topicId].TryGetOrNull(pageId);
            }

            if (result == null)
                return new GcWriterResult
                {
                    NotFound = true
                };

            if (result.AssignedPage.NotSavedAmount > 0)
                return new GcWriterResult
                {
                    NotReadyToGc = true
                };

            await result.DisposeAsync();

            return new GcWriterResult
            {
                DisposedPageWriter = result
            };
        }

        public IReadOnlyList<IPageWriter> GetLoadedWriters(string topicId)
        {
            lock (_lockObject)
            {

                if (_pageWritersCacheByTopic.TryGetValue(topicId, out var result))
                    return result.GetWriters();


                return Array.Empty<IPageWriter>();
            }
        }
    }
}