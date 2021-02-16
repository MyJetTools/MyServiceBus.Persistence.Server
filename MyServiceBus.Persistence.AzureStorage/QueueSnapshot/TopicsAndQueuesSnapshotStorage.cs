using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.AzureStorage.QueueSnapshot
{
    public class TopicsAndQueuesSnapshotStorage : ITopicsAndQueuesSnapshotStorage
    {
        private readonly IAzurePageBlob _azurePageBlob;

        public TopicsAndQueuesSnapshotStorage(IAzurePageBlob azurePageBlob)
        {
            _azurePageBlob = azurePageBlob;
        }

        private bool _initialized;
        private ValueTask CreateIfNotExistsAsync()
        {
            if (_initialized) 
                return new ValueTask();
            _initialized = true;
            return _azurePageBlob.CreateIfNotExists();
        }
        
        public async ValueTask SaveAsync(IEnumerable<TopicAndQueuesSnapshotGrpcModel> snapshot)
        {
            await CreateIfNotExistsAsync();
            
            var dataToSave = new List<TopicAndQueuesBlobContract>();
            
            foreach (var topicData in snapshot)
            {
                var topicDataToSave = TopicAndQueuesBlobContract.Create(topicData); 
                dataToSave.Add(topicDataToSave);
            }

            await _azurePageBlob.WriteAsProtobufAsync(dataToSave);
        }

        public async ValueTask<IReadOnlyList<TopicAndQueuesSnapshotGrpcModel>> GetAsync()
        {
            var result = await _azurePageBlob.ReadAndDeserializeAsProtobufAsync<List<TopicAndQueuesBlobContract>>();
            foreach (var itm in result)
                itm.Snapshots ??= Array.Empty<QueueSnapshotBlobContract>();
            
            var snapshot = result.Select(itm => itm.ToGrpcContract()).ToList();
            return snapshot;
        }
    }
}