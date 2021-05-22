using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.Metrics;

namespace MyServiceBus.Persistence.AzureStorage.TopicMessages
{
    public class PageWritersCache
    {

        private readonly Dictionary<long, PageWriter> _pageWriters
            = new ();


        private readonly Func<(string topicId, MessagePageId pageId), IAzurePageBlob> _getMessagesBlob;
        private readonly AppGlobalFlags _appGlobalFlags;
        private readonly WritePositionMetric _writePositionMetric;


        private readonly string _topicId;
        public PageWritersCache(string topicId, Func<(string topicId, MessagePageId pageId),IAzurePageBlob> getMessagesBlob, 
            AppGlobalFlags appGlobalFlags, WritePositionMetric writePositionMetric)
        {
            _topicId = topicId;
            _getMessagesBlob = getMessagesBlob;
            _appGlobalFlags = appGlobalFlags;
            _writePositionMetric = writePositionMetric;
        }

        
        public PageWriter TryGetOrNull(MessagePageId pageId)
        {
            return _pageWriters.TryGetValue(pageId.Value, out var pageWriters)
                ? pageWriters
                : null;
                
        }

        public async ValueTask<PageWriter> GetOrCreateAsync(MessagePageId pageId)
        {
            var writer = TryGetOrNull(pageId);

            if (writer != null)
                return writer;

            writer = new PageWriter(pageId, _getMessagesBlob((_topicId, pageId)), _writePositionMetric,16384);

            if (await writer.BlobExistsAsync())
                await writer.AssignPageAndInitialize(_appGlobalFlags);
            else
                await writer.CreateAndAssignAsync( _appGlobalFlags);
            
            _pageWriters.Add(pageId.Value, writer);

            return writer;
        }


        public async ValueTask<PageWriter> TryGetAsync(MessagePageId pageId)
        {
            var writer = TryGetOrNull(pageId);

            if (writer != null)
                return writer;

            writer = new PageWriter(pageId, _getMessagesBlob((_topicId, pageId)), _writePositionMetric,16384);

            if (!await writer.BlobExistsAsync())
                return null;
  
            await writer.AssignPageAndInitialize(_appGlobalFlags);
            
            _pageWriters.Add(pageId.Value, writer);

            return writer;
        }
        
        public PageWriter Remove(MessagePageId pageId)
        {
            if (_pageWriters.ContainsKey(pageId.Value))
            {
                var result = _pageWriters[pageId.Value];
                _pageWriters.Remove(pageId.Value);

                return result;
            }
            return null;
        }
        
    }
}