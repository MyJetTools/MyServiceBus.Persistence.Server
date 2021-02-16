using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs
{
    public class QueueSnapshotWriter
    {
        private readonly ITopicsAndQueuesSnapshotStorage _queueSnapshotStorage;
        private readonly QueueSnapshotCache _queueSnapshotCache;
        private readonly AppGlobalFlags _appGlobalFlags;

        public QueueSnapshotWriter(ITopicsAndQueuesSnapshotStorage queueSnapshotStorage, 
            QueueSnapshotCache queueSnapshotCache, AppGlobalFlags appGlobalFlags)
        {
            _queueSnapshotStorage = queueSnapshotStorage;
            _queueSnapshotCache = queueSnapshotCache;
            _appGlobalFlags = appGlobalFlags;
        }

        private long _savedSnapshotId = -1;
        
        private async Task ExecuteChangeAsync(long snapshotId, IReadOnlyList<TopicAndQueuesSnapshotGrpcModel> snapshot)
        {
            await _queueSnapshotStorage.SaveAsync(snapshot);
            _savedSnapshotId = snapshotId;
        }

        public void Init(long snapshotId)
        {
            _savedSnapshotId = snapshotId;
        }

        public ValueTask ExecuteAsync()
        {

            if (!_appGlobalFlags.Initialized)
                return new ValueTask();
            
            var (snapshotId, snapshotCache) = _queueSnapshotCache.Get();
            return _savedSnapshotId == snapshotId 
                ? new ValueTask() 
                : new ValueTask(ExecuteChangeAsync(snapshotId, snapshotCache));
        }
        
    }
}