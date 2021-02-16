using System;
using System.Collections.Generic;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains.MessagesContent;

namespace MyServiceBus.Persistence.AzureStorage.CompressedMessages
{



    
    public static class CompressedMessagesStorageUtils
    {

        public const int PagesOnCluster = 100;

        public static IEnumerable<MessagePageId> GetMessagePages(ClusterPageId clusterPage)
        {
            var startIndex = clusterPage.Value * PagesOnCluster;
            var endIndex = startIndex + PagesOnCluster;

            for (var i = startIndex; i < endIndex; i++)
                yield return new MessagePageId(i);
        }

        public static ClusterPageId GetClusterPageId(this MessagePageId messagePageId)
        {
            var result = messagePageId.Value / PagesOnCluster;
            return new ClusterPageId(result);
        }

        public static PagesCluster CreatePagesCluster(this IAzurePageBlob azurePageBlob, string topicId, 
            ClusterPageId clusterPageId)
        {   
            return new PagesCluster(azurePageBlob, clusterPageId, topicId);
        }

        public static MessagePageId GetFirstPageIdOnCompressedPage(this ClusterPageId clusterPage)
        {
            return new MessagePageId(clusterPage.Value * PagesOnCluster);
        }
        

        
    }
}