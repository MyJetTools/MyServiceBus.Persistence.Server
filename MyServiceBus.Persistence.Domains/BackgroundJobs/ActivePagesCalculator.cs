using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs
{


    public class ActivePagesByTopic
    {
        public TopicAndQueuesSnapshotGrpcModel Snapshot { get; internal init; }
        public IReadOnlyList<MessagePageId> Pages { get; internal init; }
    }
    

    
    public class ActivePagesCalculator
    {
        private readonly MessagesContentCache _messagesContentCache;
        private readonly QueueSnapshotCache _queueSnapshotCache;


        public ActivePagesCalculator(MessagesContentCache messagesContentCache, QueueSnapshotCache queueSnapshotCache)
        {
            _messagesContentCache = messagesContentCache;
            _queueSnapshotCache = queueSnapshotCache;
        }


        public Dictionary<string, ActivePagesByTopic> GetActivePages()
        {
            var (_, cache) = _queueSnapshotCache.Get();

            var result = new Dictionary<string, ActivePagesByTopic>();

            foreach (var topicSnapshot in cache)
            {
                var pages = new List<MessagePageId>
                {
                    MessagePageId.CreateFromMessageId(topicSnapshot.MessageId)
                };
                foreach (var queueSnapshot in topicSnapshot.QueueSnapshots)
                {
              
                    foreach (var range in queueSnapshot.Ranges)
                    {
                        var fromPageId = MessagePageId.CreateFromMessageId(range.FromId);
                        var toPageId = MessagePageId.CreateFromMessageId(range.ToId);
                    
                        if (pages.All(itm => itm.Value != fromPageId.Value))
                            pages.Add(fromPageId);

                        if (pages.All(itm => itm.Value != toPageId.Value))
                            pages.Add(toPageId);
                    }
                }
                
                result.Add(topicSnapshot.TopicId, new ActivePagesByTopic
                {
                    Snapshot = topicSnapshot,
                    Pages = pages
                });
            }

            return result;
        }
        
        
        public IEnumerable<MessagePageId> GetPagesToWarmUp(ActivePagesByTopic activePagesByTopic)
        {
            var pagesInCache = _messagesContentCache.GetLoadedPages(activePagesByTopic.Snapshot.TopicId);
            
            return pagesInCache.Where(pageInQueue => !activePagesByTopic.Pages.Any(pageInQueue.EqualsWith));
        }

        public IEnumerable<MessagePageId> GetPagesToGarbageCollect(ActivePagesByTopic activePagesByTopic)
        {
            var pagesInCache = _messagesContentCache.GetLoadedPages(activePagesByTopic.Snapshot.TopicId);
            return pagesInCache.Where(pageInCache => !activePagesByTopic.Pages.Any(pageInCache.EqualsWith));
        }
    }
}