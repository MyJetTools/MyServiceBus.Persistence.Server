using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs
{
    public static class ActivePagesGcUtils
    {
        public static IEnumerable<MessagePageId> GetPagesToWarmUp(this IReadOnlyList<MessagePageId> pagesInCache, IReadOnlyList<MessagePageId> activePages)
        {
            return activePages.Where(pageInQueue => !pagesInCache.Any(pageInQueue.EqualsWith));
        }
        
        public static IEnumerable<MessagePageId> GetPagesToGarbageCollect(this IReadOnlyList<MessagePageId> pagesInCache, IReadOnlyList<MessagePageId> activePages)
        {
            return pagesInCache.Where(pageInCache => !activePages.Any(pageInCache.EqualsWith));
        }
    }
}