using System.Collections.Generic;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.Metrics
{
    public class MaxPersistedMessageIdByTopic
    {
        private readonly Dictionary<string, long> _maxMessageId = new ();

        public Dictionary<string, long> GetSnapshot()
        {
            lock (_maxMessageId)
            {
                return new Dictionary<string, long>(_maxMessageId);
            }
        }

        public void Update(string topic, IEnumerable<MessageContentGrpcModel> messages)
        {

            lock (_maxMessageId)
            {

                foreach (var msg in messages)
                {
                    if (_maxMessageId.ContainsKey(topic))
                    {
                        if (_maxMessageId[topic] < msg.MessageId)
                        {
                            _maxMessageId[topic] = msg.MessageId;    
                        }
                    }
                    else
                    {
                        _maxMessageId.Add(topic, msg.MessageId);
                    }
                }
            }
            
        }

        public long GetOrDefault(string topicId)
        {
            lock (_maxMessageId)
            {

                if (_maxMessageId.TryGetValue(topicId, out var result))
                    return result;

                return -1;

            }
        }
    }
}