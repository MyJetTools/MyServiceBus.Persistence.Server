using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.Domains.MessagesContent
{

    
    public class MessagesContentCache
    {

        
        private readonly Dictionary<string, Dictionary<long, IMessageContentPage>> _cache 
            = new ();

        private IReadOnlyList<string> _topicsAsList = Array.Empty<string>();


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


        public WritableContentCachePage TryGetWritablePage(string topicId, MessagePageId pageId)
        {
            var result = TryGetPage(topicId, pageId);
            return result as WritableContentCachePage;
        }
        
        public WritableContentCachePage CreateWritablePage(string topicId, MessagePageId pageId)
        {
            lock (_lockObject)
            {
                if (!_cache.ContainsKey(topicId))
                    _cache.Add(topicId, new Dictionary<long, IMessageContentPage>());

                var writablePage = new WritableContentCachePage(pageId);
                _cache[topicId].Add(writablePage.PageId.Value, writablePage);
                return writablePage;
            }
        }

        
        
        public void AddPage(string topicId, IMessageContentPage page)
        {
            lock (_lockObject)
            {
                if (!_cache.ContainsKey(topicId))
                {
                    _cache.Add(topicId, new Dictionary<long, IMessageContentPage>());
                    _topicsAsList = _cache.Keys.ToList();
                }
                
                var byTopic = _cache[topicId];

                if (!byTopic.ContainsKey(page.PageId.Value))
                    byTopic.Add(page.PageId.Value, page);
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

        public IReadOnlyList<string> GetTopics()
        {
            return _topicsAsList;
        }

        public IReadOnlyList<WritableContentCachePage> GetWritablePagesHasMessagesToUpload(string topicId)
        {
            List<WritableContentCachePage> result = null;
            lock (_lockObject)
            {

                if (_cache.TryGetValue(topicId, out var pagesByTopic))
                {

                    foreach (var messageContentPage in pagesByTopic.Values)
                    {
                        if (messageContentPage is not WritableContentCachePage writableContentCachePage) 
                            continue;


                        if (writableContentCachePage.NotSavedAmount > 0)
                        {
                            result ??= new List<WritableContentCachePage>();
                            result.Add(writableContentCachePage);
                        }
                    }

                }

                return (IReadOnlyList<WritableContentCachePage>)result ?? Array.Empty<WritableContentCachePage>();
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