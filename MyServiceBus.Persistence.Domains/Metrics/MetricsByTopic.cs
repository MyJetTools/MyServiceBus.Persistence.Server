using System;
using System.Collections.Generic;
using MyServiceBus.Persistence.Domains.MessagesContent;

namespace MyServiceBus.Persistence.Domains.Metrics
{

    public class TopicMetrics
    {
        public MessagePageId PageId { get; private set; }
        
        public long BlobPosition { get; private set; }
        
        public long MaxSavedMessageId { get; private set; }
        
        public int LastSavedChunk { get; set; }
        public TimeSpan LastSaveDuration { get; set; }
        public DateTime LastSaveMoment { get; set; } = DateTime.UtcNow;

        public void Update(MessagePageId pageId, long blobPosition, long maxSavedMessageId)
        {

            if (MaxSavedMessageId < maxSavedMessageId)
                MaxSavedMessageId = maxSavedMessageId;
            
            if (PageId.Value <= pageId.Value)
            {
                BlobPosition = blobPosition;
                PageId = pageId;
            }
                
        }
    }
    
    public class MetricsByTopic
    {
        
        private readonly object _lockObject = new ();

        private Dictionary<string, TopicMetrics> _positions = new ();


        public TopicMetrics Get(string topicId)
        {

            if (_positions.TryGetValue(topicId,  out var result))
              return result;

            lock (_lockObject)
            {
                if (_positions.TryGetValue(topicId, out var writePosition))
                    return writePosition;

                var newInstance = new Dictionary<string, TopicMetrics>(_positions);

                writePosition = new TopicMetrics();
                newInstance.Add(topicId, writePosition);
                _positions = newInstance;
                
                return writePosition;
            }
        }

        public Dictionary<string, TopicMetrics> Get()
        {
            return _positions;
        }
    }
}