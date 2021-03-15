using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.Domains.MessagesContent
{
    public class TopicDataLocator
    {
        public readonly AsyncLock AsyncLock = new ();

        private readonly object _lockObject = new ();

        private readonly IDictionary<long, IMessageContentPage> _contentPages 
            = new Dictionary<long, IMessageContentPage>();

        private IReadOnlyList<IMessageContentPage> _contentPagesAsList = Array.Empty<IMessageContentPage>();

        public readonly MessagesToPersist MessagesToPersist = new ();
        
        public string TopicId { get; }

        public TopicDataLocator(string topicId)
        {
            TopicId = topicId;
        }

        public IMessageContentPage TryGetPage(MessagePageId pageId)
        {
            lock (_lockObject)
            {
                return _contentPages.TryGetValueOrDefault(pageId.Value);    
            }
        }

        public WritableContentCachePage GetOrCreateWritablePage(MessagePageId pageId)
        {
            lock (_lockObject)
            {
                var result = TryGetPage(pageId);
                if (result is WritableContentCachePage writableContentCachePage)
                    return writableContentCachePage;
                
                if (result != null)
                {
                    writableContentCachePage = new WritableContentCachePage(pageId, result.GetMessages());
                    _contentPages[pageId.Value] = writableContentCachePage;
                    _contentPagesAsList = _contentPages.Values.ToList();
                    return writableContentCachePage;
                }

                writableContentCachePage = new WritableContentCachePage(pageId);
                _contentPages.Add(pageId.Value, writableContentCachePage);
                _contentPagesAsList = _contentPages.Values.ToList();
                return writableContentCachePage;
            }
        }

        public void AddPage(MessagePageId pageId, IMessageContentPage page)
        {
            lock (_lockObject)
            {
                if (_contentPages.ContainsKey(pageId.Value))
                    return;
                _contentPages.Add(pageId.Value, page);
                _contentPagesAsList = _contentPages.Values.ToList();
            }
        }

        public IReadOnlyList<IMessageContentPage> GetLoadedPages()
        {
            return _contentPagesAsList;
        }


        public void RemovePage(MessagePageId pageId)
        {
            lock (_lockObject)
            {
                _contentPages.Remove(pageId.Value);
                _contentPagesAsList = _contentPages.Values.ToList();
            }
        }

    }
}