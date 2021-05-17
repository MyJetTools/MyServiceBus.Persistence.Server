using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Server.Grpc
{
    public class MyServiceBusQueuePersistenceGrpcService : IMyServiceBusQueuePersistenceGrpcService
    {
        public ValueTask SaveSnapshotAsync(SaveQueueSnapshotGrpcRequest request)
        {
            if (ServiceLocator.AppGlobalFlags.IsShuttingDown)
                throw new Exception("App is stopping");

            request.QueueSnapshot ??= Array.Empty<TopicAndQueuesSnapshotGrpcModel>();

            foreach (var queueSnapshot in request.QueueSnapshot)
                queueSnapshot.QueueSnapshots ??= Array.Empty<QueueSnapshotGrpcModel>();

            ServiceLocator.QueueSnapshotCache.Save(request.QueueSnapshot);
            
            return new ValueTask();
        }

        public async IAsyncEnumerable<TopicAndQueuesSnapshotGrpcModel> GetSnapshotAsync()
        {
            if (!ServiceLocator.AppGlobalFlags.Initialized)
                throw new Exception("App is not initialized yet");

            var snapshot = await ServiceLocator.QueueSnapshotCache.GetAsync();

            foreach (var queuesInTopicSnapshot in snapshot)
                yield return queuesInTopicSnapshot;
        }

    }
}