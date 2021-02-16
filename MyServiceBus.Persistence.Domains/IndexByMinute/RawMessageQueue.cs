using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.IndexByMinute
{
    internal static class RawMessageQueue
    {
        
        internal static void Enqueue(this Dictionary<string, List<MessageContentGrpcModel>> queue, string topicId, IEnumerable<MessageContentGrpcModel> grpcModels)
        {
            lock (queue)
            {
                if (!queue.ContainsKey(topicId))
                    queue.Add(topicId, new List<MessageContentGrpcModel>());
                
                queue[topicId].AddRange(grpcModels);
            }
        }
        
        internal static (string topicId, List<MessageContentGrpcModel> messages) Dequeue(this Dictionary<string, List<MessageContentGrpcModel>> queue)
        {
            lock (queue)
            {
                if (queue.Count == 0)
                    return (null, null);

                var (topicId, messages) = queue.First();

                queue.Remove(topicId);

                return (topicId, messages);
            }
            
        }

    }
}