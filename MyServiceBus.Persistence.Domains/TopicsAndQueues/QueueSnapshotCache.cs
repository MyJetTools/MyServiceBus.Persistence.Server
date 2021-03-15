using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.TopicsAndQueues
{
    public class QueueSnapshotCache
    {
        private IReadOnlyList<TopicAndQueuesSnapshotGrpcModel> _cache = new List<TopicAndQueuesSnapshotGrpcModel>();
        
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ();

        private long _snapshotId;

        public (long SnapshotId, IReadOnlyList<TopicAndQueuesSnapshotGrpcModel> Cache) Get()
        {
            
            _readerWriterLockSlim.EnterReadLock();
            try
            {
                return (_snapshotId, _cache);
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }

        }

        public ValueTask<IReadOnlyList<TopicAndQueuesSnapshotGrpcModel>> GetAsync()
        {
            _readerWriterLockSlim.EnterReadLock();
            try
            {
                return new ValueTask<IReadOnlyList<TopicAndQueuesSnapshotGrpcModel>>(_cache);
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public void Save(IReadOnlyList<TopicAndQueuesSnapshotGrpcModel> queueSnapshotGrpcModels)
        {
            _readerWriterLockSlim.EnterWriteLock();
            try
            {
                _cache = queueSnapshotGrpcModels;
                _snapshotId++;
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        public void Init(IReadOnlyList<TopicAndQueuesSnapshotGrpcModel> queueSnapshotGrpcModels, long snapshotId)
        {
            _readerWriterLockSlim.EnterWriteLock();
            try
            {
                _cache = queueSnapshotGrpcModels;
                _snapshotId = snapshotId;
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }
    }
}