using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.Domains.MessagesContent
{


    public class CreateWritablePageResult
    {
        public IMessageContentPage Exists { get; set; }
        
        public WritableContentPage Result { get; set; }
    }
    
    public class MessagesContentCache
    {

        
        private readonly Dictionary<string, Dictionary<long, IMessageContentPage>> _cache 
            = new ();


        private readonly object _lockObject = new();

        public IMessageContentPage TryGetPage(string topicId, MessagePageId pageId)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(topicId, out var byTopic))
                    return byTopic.TryGetValue(pageId.Value, out var result) ? result : null;

                return null;
            }
        }

        public WritableContentPage TryGetWritablePage(string topicId, MessagePageId pageId)
        {
            var result = TryGetPage(topicId, pageId);
            return result as WritableContentPage;
        }
        
        public CreateWritablePageResult CreateWritablePage(string topicId, MessagePageId pageId)
        {
            lock (_lockObject)
            {
                if (!_cache.ContainsKey(topicId))
                    _cache.Add(topicId, new Dictionary<long, IMessageContentPage>());

                var byTopic = _cache[topicId];

                if (byTopic.TryGetValue(pageId.Value, out var result))
                {
                    return new CreateWritablePageResult
                    {
                        Exists = result
                    };
                }

                var writablePage = new WritableContentPage(pageId);
                _cache[topicId].Add(writablePage.PageId.Value, writablePage);
                return new CreateWritablePageResult
                {
                    Result = writablePage
                };
            }
        }

        
        
        public void AddPage(string topicId, IMessageContentPage page)
        {
            lock (_lockObject)
            {
                if (!_cache.ContainsKey(topicId))
                {
                    _cache.Add(topicId, new Dictionary<long, IMessageContentPage>());
                }
                
                var byTopic = _cache[topicId];

                if (!byTopic.ContainsKey(page.PageId.Value))
                    byTopic.Add(page.PageId.Value, page);
            }
        }
        
        public WritableContentPage ConvertIntoWritable(string topicId, IMessageContentPage page)
        {
            lock (_lockObject)
            {
                if (!_cache.ContainsKey(topicId))
                    _cache.Add(topicId, new Dictionary<long, IMessageContentPage>());
                
                var byTopic = _cache[topicId];

                if (!byTopic.ContainsKey(page.PageId.Value))
                    byTopic.Remove(page.PageId.Value);


                var writablePage = new WritableContentPage(page.PageId);
                writablePage.Init(page.GetMessages());
                
                byTopic.Add(page.PageId.Value, writablePage);

                return writablePage;
            }
            
        }

        
        public Dictionary<string, (int loadedPages, long contentSize)> GetMetrics()
        {
            var result = new Dictionary<string, (int loadedPages, long contentSize)>();
            lock (_lockObject)
            {
                foreach (var (topicId, pagesByTopic) in _cache)
                    result.Add(topicId, (pagesByTopic.Count, pagesByTopic.Values.Sum(itm => itm.TotalContentSize)));
            }
            return result;
        }


        public SortedDictionary<string, IReadOnlyList<IMessageContentPage>> GetLoadedPages()
        {

            var result = new SortedDictionary<string, IReadOnlyList<IMessageContentPage>>();
            lock (_lockObject)
            {
                foreach (var (topicId, pagesByTopic) in _cache)
                    result.Add(topicId, pagesByTopic.Values.ToList());
            }
            return result;
        }
        
        public IReadOnlyList<IMessageContentPage> GetLoadedPages(string topicId)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(topicId,out var pagesByTopic))
                    return pagesByTopic.Values.ToList();
            }
            return Array.Empty<IMessageContentPage>();
        }

        public void DisposePage(string topicId, in MessagePageId pageId)
        {
            lock (_lockObject)
            {
                if (!_cache.TryGetValue(topicId, out var pagesByTopic)) 
                    return;
                
                if (pagesByTopic.ContainsKey(pageId.Value))
                    pagesByTopic.Remove(pageId.Value);

            }
        }


        public bool HasPage(string topicId, MessagePageId pageId)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(topicId, out var result))
                    return result.ContainsKey(pageId.Value);

                return false;
            }
        }
    }
}