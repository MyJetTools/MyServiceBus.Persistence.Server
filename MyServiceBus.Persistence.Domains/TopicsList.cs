using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent;

namespace MyServiceBus.Persistence.Domains
{
    public class TopicsList
    {
        private readonly object _lockObject = new();

        private readonly Dictionary<string, TopicDataLocator> _dataLocators = new ();

        private void UpdateList()
        {
            AllDataLocators = _dataLocators.Values.ToList();
            SnapshotId++;
        }
        public TopicDataLocator GetOrCreate(string topicId)
        {
            lock (_lockObject)
            {
                var result = _dataLocators.TryGetValueOrDefault(topicId);

                if (result != default)
                    return result;

                var newLocator = new TopicDataLocator(topicId);
                _dataLocators.Add(topicId, newLocator);
                UpdateList();
                return newLocator;
            }
        }

        public TopicDataLocator TryGet(string topicId)
        {
            lock (_lockObject)
            {
                return _dataLocators.TryGetValueOrDefault(topicId);  
            }
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
                foreach (var topicId in topics)
                    _dataLocators.Add(topicId, new TopicDataLocator(topicId));
                UpdateList();
            }
        }
    }
}