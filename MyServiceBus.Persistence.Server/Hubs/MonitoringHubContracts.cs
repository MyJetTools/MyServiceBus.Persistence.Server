using System;
using System.Collections.Generic;

namespace MyServiceBus.Persistence.Server.Hubs
{
    public class TopicHubModel
    {
        public string Id { get; set; }
    }

    public class TopicPageHubModel
    {
        public long Size { get; set; }
        public int Percent { get; set; }

        public bool IsTheSameWith(TopicPageHubModel itm)
        {

            if (itm.Size != Size)
                return false;
            
            if (itm.Percent != Percent)
                return false;

            return true;
        }
    }

    public class TopicHubInfoModel
    {
        public long WritePosition { get; set; }
        
        public long MessageId { get; set; }
        
        public int WriteQueueSize { get; set; }
        public IDictionary<string, TopicPageHubModel> Pages { get; set; }

        public bool IsTheSameWith(TopicHubInfoModel info)
        {

            if (info.WriteQueueSize != WriteQueueSize)
                return false;
            
            if (info.WritePosition != WritePosition)
                return false;
            
            return info.MessageId == MessageId
                   && Pages.ItemsAreSame(info.Pages, (src, dest) => src.IsTheSameWith(dest));
        }
    }


    public static class Extensions
    {
        public static bool ItemsAreSame<TKey, TValue>(this IDictionary<TKey, TValue> src, 
            IDictionary<TKey, TValue> dest, Func<TValue, TValue, bool> areSame)
        {
            
            if (src.Count != dest.Count)
                return false;

            foreach (var (srcKey, srcValue) in src)
            {
                if (!dest.TryGetValue(srcKey, out var destValue))
                    return false;

                if (!areSame(srcValue, destValue))
                    return false;
            }

            return true;
        }
    }
    
    
}