using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.AzureStorage.TopicMessages;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Server.Hubs
{
    public static class MonitoringHubContractsBuilders
    {

        public static Dictionary<string, TopicHubInfoModel> BuildTopicHubInfoModels(
            this IEnumerable<TopicAndQueuesSnapshotGrpcModel> topicSnapshots)
        {
            var result = new Dictionary<string, TopicHubInfoModel>();

            foreach (var topicSnapshot in topicSnapshots)
            {

                var topicData = ServiceLocator.TopicsList.TryGet(topicSnapshot.TopicId);
                
                var loadedPages = topicData == null ? Array.Empty<IMessageContentPage>() : topicData.GetLoadedPages(); 

                var model = new TopicHubInfoModel
                {
                    MessageId = topicSnapshot.MessageId,
                    WritePosition = PageWriter.GetWritePosition(topicSnapshot.TopicId),
                    WriteQueueSize = topicData?.MessagesToPersist.MessagesToWrite() ?? 0,
                    Pages = loadedPages.ToDictionary(
                        itm => itm.PageId.Value.ToString(), 
                        itm => new TopicPageHubModel
                    {
                        Size = itm.TotalContentSize,
                        Percent = (int)(itm.Count * 0.001)
                    })
                };
                
                result.Add(topicSnapshot.TopicId, model);
            }

            return result;
        }



        public static Dictionary<string, object> ToDifferenceHubContract<TValue>(
            this DictionaryDifferenceResult<string, TValue> src)
        {

            var result = new Dictionary<string, object>();

            if (src.Inserted != null)
                result.Add("i", src.Inserted);
            
            if (src.Updated != null)
                result.Add("u", src.Updated);
            
            if (src.Deleted != null)
                result.Add("d", src.Deleted.Keys.ToList());

            return result;

        }
        
    }

  
}