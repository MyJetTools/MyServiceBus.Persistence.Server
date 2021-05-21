using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.Domains.MessagesContent
{

    
    public class MessagesContentCache
    {

        public class TopicContentGroup
        {
            public TopicContentGroup(string topicId)
            {
                TopicId = topicId;
            }
            public string TopicId { get; }
            public readonly Dictionary<long, IMessageContentPage> Dictionary = new ();
            
        }
        
        private readonly Dictionary<string, TopicContentGroup> _cache 
            = new ();

        private IReadOnlyList<string> _topicsAsList = Array.Empty<string>();


        public IMessageContentPage TryGetPage(string topicId, MessagePageId pageId)
        {
            lock (_cache)
            {
                if (!_cache.ContainsKey(topicId))
                    return null;

                return _cache[topicId].Dictionary.TryGetValue(pageId.Value, out var result) ? result : null;
            }
        }

        
        
        public IMessageContentPage AddPage(string topicId, IMessageContentPage page)
        {
            lock (_cache)
            {
                if (!_cache.ContainsKey(topicId))
                {
                    _cache.Add(topicId, new TopicContentGroup(topicId));
                    _topicsAsList = _cache.Keys.ToList();
                }
                    
                
                var topicContentGroup = _cache[topicId];

                if (topicContentGroup.Dictionary.TryGetValue(page.PageId.Value, out var foundPage))
                    return foundPage;
                
                topicContentGroup.Dictionary.Add(page.PageId.Value, page);
                return page;
            }
        }

        
        public Dictionary<string, (int loadedPages, long contentSize)> GetMetrics()
        {
            var result = new Dictionary<string, (int loadedPages, long contentSize)>();
            lock (_cache)
            {
                foreach (var group in _cache.Values)
                    result.Add(group.TopicId, (group.Dictionary.Count, group.Dictionary.Values.Sum(itm => itm.TotalContentSize)));
            }
            return result;
        }


        public Dictionary<string, IReadOnlyList<long>> GetLoadedPages()
        {

            var result = new Dictionary<string, IReadOnlyList<long>>();
            lock (_cache)
            {
                foreach (var group in _cache.Values)
                    result.Add(group.TopicId, group.Dictionary.Keys.ToList());
            }
            return result;
        }
        
        public IReadOnlyList<MessagePageId> GetLoadedPages(string topicId)
        {
            lock (_cache)
            {
                if (_cache.ContainsKey(topicId))
                    return _cache[topicId].Dictionary.Keys.Select(itm => new MessagePageId(itm)).ToList();
            }
            return Array.Empty<MessagePageId>();
        }

        public void DisposePage(string topicId, in MessagePageId pageId)
        {
            lock (_cache)
            {
                if (!_cache.ContainsKey(topicId))
                    return;

                var group = _cache[topicId];

                if (group.Dictionary.ContainsKey(pageId.Value))
                    group.Dictionary.Remove(pageId.Value);
            }
        }

        public IReadOnlyList<string> GetTopics()
        {
            return _topicsAsList;
        }
    }
}