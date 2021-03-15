using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace MyServiceBus.Persistence.Server.Hubs
{
    public static class MonitoringHubExtensions
    {

        public static ValueTask SendInitAsync(this MonitoringHubConnection connection)
        {
            var model = new
            {
                Version = ServiceLocator.AppVersion.Value
            };
            var task = connection.ClientProxy.SendAsync("init", model);
            return new ValueTask(task);
        }

        public static ValueTask SendTopicsAsync(this MonitoringHubConnection connection)
        {
            
            var (dataLocators, snapshotId) = ServiceLocator.TopicsList.GetSnapshot();
            if (connection.LastTopicSnapshotId == snapshotId)
                return new ValueTask();


            var topicsHubModel = dataLocators
                .Select(tdl => new TopicHubModel
                {
                    Id = tdl.TopicId
                });

            var task = connection.ClientProxy.SendAsync("topics", topicsHubModel);
            connection.LastTopicSnapshotId = snapshotId;
            return new ValueTask(task);
            
        }


        public static ValueTask SendTopicsInfo(this MonitoringHubConnection connection)
        {

            var (_, cache) = ServiceLocator.QueueSnapshotCache.Get();

            var topicInfoModel = cache.BuildTopicHubInfoModels();

            var difference = connection.GetTopicInfoDifference(topicInfoModel);

            if (difference == null)
                return new ValueTask();

            var contract = new Dictionary<string, TopicHubInfoModel>();

            if (difference.Inserted != null)
                foreach (var (topicId, itm) in difference.Inserted)
                    contract.Add(topicId, itm);

            if (difference.Updated != null)
                foreach (var (topicId, itm) in difference.Updated)
                    contract.Add(topicId, itm);

            var task = connection.ClientProxy.SendAsync("topics-info", contract);

            return new ValueTask(task);

        }

    }
}