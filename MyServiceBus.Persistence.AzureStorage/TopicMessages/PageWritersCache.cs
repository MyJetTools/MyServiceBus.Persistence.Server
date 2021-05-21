using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncUtilities;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using ValueTask = AsyncUtilities.ValueTask;

namespace MyServiceBus.Persistence.AzureStorage.TopicMessages
{
    public class PageWritersCache
    {

        private readonly Dictionary<long, PageWriter> _pageWriters
            = new ();


        private readonly Func<(string topicId, MessagePageId pageId), IAzurePageBlob> _getMessagesBlob;
        private readonly AppGlobalFlags _appGlobalFlags;


        private readonly string _topicId;
        public PageWritersCache(string topicId, Func<(string topicId, MessagePageId pageId),IAzurePageBlob> getMessagesBlob, AppGlobalFlags appGlobalFlags)
        {
            _topicId = topicId;
            _getMessagesBlob = getMessagesBlob;
            _appGlobalFlags = appGlobalFlags;
        }

        
        private PageWriter TryGet(MessagePageId pageId)
        {
            return _pageWriters.TryGetValue(pageId.Value, out var pageWriters)
                ? pageWriters
                : null;
                
        }


        public async ValueTask<PageWriter> GetOrCreateAsync(MessagePageId pageId)
        {
            var writer = TryGet(pageId);

            if (writer != null)
                return writer;

            writer = new PageWriter(_topicId, pageId, _getMessagesBlob((_topicId, pageId)), 16384);

            if (await writer.BlobExistsAsync())
                await writer.AssignPageAndInitialize(new WritableContentCachePage(pageId), _appGlobalFlags);
            else
                await writer.CreateAndAssignAsync(new WritableContentCachePage(pageId));
            
            _pageWriters.Add(pageId.Value, writer);

            return writer;
        }
        
        public async ValueTask<PageWriter> TryGetAsync(MessagePageId pageId, Func<WritableContentCachePage> createCachePage)
        {

            var writer = TryGet(pageId);

            if (writer != null)
                return writer;

            writer = new PageWriter(_topicId, pageId, _getMessagesBlob((_topicId, pageId)), 16384);

            if (!await writer.BlobExistsAsync())
                return null;
  
            await writer.AssignPageAndInitialize(createCachePage(), _appGlobalFlags);
            
            _pageWriters.Add(pageId.Value, writer);

            return writer;
        }


        public IEnumerable<PageWriter> GetPageWritersWeHaveToSync()
        {
            return _pageWriters.Values.Where(pageWriter => pageWriter.HasToSync());
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