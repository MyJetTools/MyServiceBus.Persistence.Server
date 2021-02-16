using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.TopicsAndQueues
{
    public interface ITopicsAndQueuesSnapshotStorage
    {
        ValueTask SaveAsync(IEnumerable<TopicAndQueuesSnapshotGrpcModel> snapshot);
        ValueTask<IReadOnlyList<TopicAndQueuesSnapshotGrpcModel>> GetAsync();
    }
}