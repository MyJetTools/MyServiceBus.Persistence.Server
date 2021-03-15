using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent;

namespace MyServiceBus.Persistence.Domains
{
    public class TopicsList
    {
        private readonly object _lockObject = new();

        private IDictionary<string, TopicDataLocator> _dataLocators = new Dictionary<string, TopicDataLocator>();

        private void UpdateList()
        {
            AllDataLocators = _dataLocators.Values.ToList();
            SnapshotId++;
        }
        public TopicDataLocator GetOrCreate(string topicId)
        {
            var result = _dataLocators.TryGetValueOrDefault(topicId);

            if (result != default)
                return result;


            lock (_lockObject)
            {
                result = _dataLocators.TryGetValueOrDefault(topicId);
                if (result != default)
                    return result;

                var newLocator = new TopicDataLocator(topicId);
                _dataLocators = _dataLocators.AddByCreatingNewDictionary(topicId, ()=>newLocator);
                UpdateList();
                return newLocator;
            }
        }

        public TopicDataLocator TryGet(string topicId)
        {
            return _dataLocators.TryGetValueOrDefault(topicId);
        }


        public IReadOnlyList<TopicDataLocator> AllDataLocators { get; private set; } = Array.Empty<TopicDataLocator>();
        public long SnapshotId { get; private set; }


        public (IReadOnlyList<TopicDataLocator> dataLocators, long snapshotId) GetSnapshot()
        {
            lock (_lockObject)
            {
                return (AllDataLocators, SnapshotId);
            }
        }

        public void Init(IEnumerable<string> topics)
        {
            lock (_lockObject)
            {
                var result = topics
                    .ToDictionary(topic => topic, 
                    topic => new TopicDataLocator(topic));

                _dataLocators = result;
                UpdateList();
            }
        }
    }
}