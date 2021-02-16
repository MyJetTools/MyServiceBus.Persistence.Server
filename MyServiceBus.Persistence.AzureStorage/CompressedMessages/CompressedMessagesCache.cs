using System.Collections.Generic;
using System.Linq;

namespace MyServiceBus.Persistence.AzureStorage.CompressedMessages
{
    public static class CompressedMessagesCache
    {
        public static PagesCluster FindInCache(this List<PagesCluster> cache, string topicId, ClusterPageId clusterPageId)
        {
            lock (cache)
            {
                var result = cache.FirstOrDefault(itm => itm.TopicId == topicId && itm.ClusterPageId.EqualsWith(clusterPageId));

                if (result == null)
                    return null;

                var index = cache.IndexOf(result);
                if (index != 0)
                {
                    cache.Remove(result);
                    cache.Insert(0,result);
                }

                return result;
            }
        }

        public static void AddToCache(this List<PagesCluster> cache, PagesCluster page)
        {
            lock (cache)
            {
                cache.Insert(0, page);
            }
        }
    }
}