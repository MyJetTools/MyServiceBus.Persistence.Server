using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using MyServiceBus.Persistence.Domains;

namespace MyServiceBus.Persistence.Server.Hubs
{
    public class MonitoringHubConnection
    {
        public IClientProxy ClientProxy { get; }
        
        public string Id { get; }

        public MonitoringHubConnection(string id, IClientProxy clientProxy)
        {
            ClientProxy = clientProxy;
            Id = id;
        }

        private readonly object _lockObject = new();

        public long LastTopicSnapshotId { get; set; } = -1;

        private Dictionary<string, TopicHubInfoModel> _lastSendTopicInfoSnapshot;
        
        
        public DictionaryDifferenceResult<string, TopicHubInfoModel> GetTopicInfoDifference(Dictionary<string, TopicHubInfoModel> topicInfo)
        {

            lock (_lockObject)
            {
                if (_lastSendTopicInfoSnapshot == null)
                {
                    _lastSendTopicInfoSnapshot = topicInfo;
                    return DictionaryDifferenceResult<string, TopicHubInfoModel>.CreateAsFirstIteration(topicInfo);
                }

                var result = _lastSendTopicInfoSnapshot.GetTheDifference(topicInfo, 
                    (now, next) => now.IsTheSameWith(next));

                if (result != null)
                    _lastSendTopicInfoSnapshot = topicInfo;

                return result;
            }

        }
    }


    public class MonitoringHubConnectionList
    {
        private IDictionary<string, MonitoringHubConnection> _connections = new Dictionary<string, MonitoringHubConnection>();

        private readonly object _lockObject = new();

        public void Add(MonitoringHubConnection connection)
        {

            lock (_lockObject)
            {
                _connections = _connections.AddByCreatingNewDictionary(connection.Id, ()=>connection);
            }
        }

        public void Remove(string id)
        {
            lock (_lockObject)
            {
                _connections = _connections.RemoveByCreatingNewDictionary(id);
            }
            
        }
    }
}