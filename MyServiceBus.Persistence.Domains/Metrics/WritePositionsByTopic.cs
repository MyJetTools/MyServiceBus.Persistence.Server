using System.Collections.Generic;
using MyServiceBus.Persistence.Domains.MessagesContent;

namespace MyServiceBus.Persistence.Domains.Metrics
{

    public class WritePositionMetric
    {
        public MessagePageId PageId { get; private set; }
        
        public long Position { get; private set; }

        public void Update(MessagePageId pageId, long writePositionIndex)
        {
            if (PageId.Value >= pageId.Value)
            {
                Position = writePositionIndex;
                PageId = pageId;
            }
                
        }
    }
    
    public class WritePositionsByTopic
    {
        
        private readonly object _lockObject = new object();

        private Dictionary<string, WritePositionMetric> _positions = new ();


        public WritePositionMetric GetWritePositionMetric(string topicId)
        {

            if (_positions.TryGetValue(topicId,  out var result))
              return result;

            lock (_lockObject)
            {
                if (_positions.TryGetValue(topicId, out var writePosition))
                    return writePosition;

                var newInstance = new Dictionary<string, WritePositionMetric>(_positions);

                writePosition = new WritePositionMetric();
                newInstance.Add(topicId, writePosition);
                _positions = newInstance;
                
                return writePosition;
            }
        }
    }
}