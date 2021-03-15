using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.TopicsAndQueues
{
    public class TopicAndQueueInitializer
    {
        private readonly QueueSnapshotCache _queueSnapshotCache;
        private readonly ITopicsAndQueuesSnapshotStorage _storage;
        private readonly QueueSnapshotWriter _queueSnapshotWriter;
        private readonly AppGlobalFlags _appGlobalFlags;
        private readonly IAppLogger _appLogger;
        private readonly TopicsList _topicsList;

        public TopicAndQueueInitializer(QueueSnapshotCache queueSnapshotCache,
            ITopicsAndQueuesSnapshotStorage storage, QueueSnapshotWriter queueSnapshotWriter, 
            AppGlobalFlags appGlobalFlags, IAppLogger appLogger, TopicsList topicsList)
        {
            _queueSnapshotCache = queueSnapshotCache;
            _storage = storage;
            _queueSnapshotWriter = queueSnapshotWriter;
            _appGlobalFlags = appGlobalFlags;
            _appLogger = appLogger;
            _topicsList = topicsList;
        }

        private async Task<IReadOnlyList<TopicAndQueuesSnapshotGrpcModel>> InitQueueSnapshot()
        {
            try
            {
                return await _storage.GetAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw ;
            }
        }


        public async Task InitAsync()
        {

            const int snapshotId = 0;
            
            var fullSnapshot = await InitQueueSnapshot();
            _appLogger.AddLog("SYSTEM", "Queues snapshot initialized ok");
            
            _queueSnapshotCache.Init(fullSnapshot, snapshotId);
            _queueSnapshotWriter.Init(snapshotId);

            _topicsList.Init(fullSnapshot.Select(itm => itm.TopicId));

            _appGlobalFlags.Initialized = true;
            _appLogger.AddLog("SYSTEM", "Application Initialized");
        }
  
    }
}