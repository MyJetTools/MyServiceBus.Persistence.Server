using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;

namespace MyServiceBus.Persistence.Domains.MessagesContentCompressed
{
    public class CompressedMessagesUtils
    {
        private readonly QueueSnapshotCache _queueSnapshotCache;

        public CompressedMessagesUtils(QueueSnapshotCache queueSnapshotCache)
        {
            _queueSnapshotCache = queueSnapshotCache;
        }

        public bool PageCanBeCompressed(string topicId, MessagePageId pageId)
        {
            var snapshot = _queueSnapshotCache.Get();

            var topicSnapshot = snapshot.Cache.FirstOrDefault(itm => itm.TopicId == topicId);

            if (topicSnapshot == null)
                return false;

            var activePageId = MessagesContentPagesUtils.GetPageId(topicSnapshot.MessageId);

            return pageId.Value<activePageId.Value - 1;
        }


    }
}