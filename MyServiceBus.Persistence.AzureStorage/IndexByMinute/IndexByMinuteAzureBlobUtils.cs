using System.Collections.Generic;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains.IndexByMinute;

namespace MyServiceBus.Persistence.AzureStorage.IndexByMinute
{
    public static class IndexByMinuteAzureBlobUtils
    {

        public static IEnumerable<int> GetAffectedPages(IEnumerable<int> minutes)
        {
            var result = new Dictionary<int, int>();
            foreach (var minute in minutes)
            {
                var offset = MessagesMinuteUtils.GetIndexOffset(minute);

                var page = offset / MyAzurePageBlobUtils.PageSize;
                
                if (!result.ContainsKey(page))
                    result.Add(page, page);
            }

            return result.Keys;
        }
        
    }
}