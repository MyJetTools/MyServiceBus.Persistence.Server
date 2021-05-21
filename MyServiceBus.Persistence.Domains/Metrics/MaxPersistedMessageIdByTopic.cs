using System.Collections.Generic;

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

        public void Update(string topic, long messageId)
        {

            lock (_maxMessageId)
            {
                if (_maxMessageId.ContainsKey(topic))
                {
                    _maxMessageId[topic] = messageId;
                }
                else
                {
                    _maxMessageId.Add(topic, messageId);
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