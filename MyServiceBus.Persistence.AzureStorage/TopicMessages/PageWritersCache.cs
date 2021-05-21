using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent;

namespace MyServiceBus.Persistence.AzureStorage.TopicMessages
{
    public class PageWritersCache
    {

        private readonly Dictionary<string, List<PageWriter>> _pageWriters
            = new Dictionary<string, List<PageWriter>>();

        private void GcPageWriter(List<PageWriter> topicPageWriters)
        {

            while (topicPageWriters.Count>3)
            {
                var minId = topicPageWriters.Min(itm => itm.PageId.Value);

                var index = topicPageWriters.FindIndex(itm => itm.PageId.EqualsWith(minId));
                
                topicPageWriters.RemoveAt(index);
            }
            
        }


        public PageWriter TryGet(string topicId, MessagePageId pageId)
        {
            lock (_pageWriters)
            {
                return _pageWriters.TryGetValue(topicId, out var pageWriters)
                ? pageWriters.FirstOrDefault(itm => itm.PageId.EqualsWith(pageId))
                : null;
            }
        }
    }
}