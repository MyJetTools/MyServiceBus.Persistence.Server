using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.TopicsAndQueues
{
    public static class TopicsAndQueuesUtils
    {


        private static void PopulatePages(this List<MessagePageId> pages, MessagePageId pageId, MessagePageId maxPageId)
        {
            if (pageId.Value <= maxPageId.Value)
                if (pages.All(itm => itm.Value != pageId.Value))
                    pages.Add(pageId);


            var nextPageId = pageId.Value + 1;
            if (nextPageId <= maxPageId.Value)
                if (pages.All(itm => itm.Value != nextPageId))
                    pages.Add(new MessagePageId(nextPageId));
            
            var prevPageId = pageId.Value - 1;
            if (prevPageId<0)
                return;
            
            if (prevPageId <= maxPageId.Value)
                if (pages.All(itm => itm.Value != prevPageId))
                    pages.Add(new MessagePageId(prevPageId));
        }



        public static IReadOnlyList<MessagePageId> GetActivePages(this TopicAndQueuesSnapshotGrpcModel snapshot)
        {

            var result = new List<MessagePageId>();
            
            var maxPageId = MessagesContentPagesUtils.GetPageId(snapshot.MessageId);
                
            result.PopulatePages(maxPageId, maxPageId);

            foreach (var queueSnapshot in snapshot.QueueSnapshots)
            {

                if (queueSnapshot.Ranges != null)
                    foreach (var range in queueSnapshot.Ranges)
                    {
                        var pageId = MessagesContentPagesUtils.GetPageId(range.FromId);

                        result.PopulatePages(pageId, maxPageId);

                        pageId = MessagesContentPagesUtils.GetPageId(range.ToId);
                        result.PopulatePages(pageId, maxPageId);
                    }
            }

            return result;
        }
        
        
        
    }
}