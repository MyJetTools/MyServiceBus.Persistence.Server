using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.PersistenceOperations;
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
        private readonly TaskSchedulerByTopic _schedulerByTopic;
        private readonly RestorePageFromBlobOperation _restorePageFromBlobOperation;

        public TopicAndQueueInitializer(QueueSnapshotCache queueSnapshotCache,
            ITopicsAndQueuesSnapshotStorage storage, QueueSnapshotWriter queueSnapshotWriter, 
             AppGlobalFlags appGlobalFlags, IAppLogger appLogger, TaskSchedulerByTopic schedulerByTopic,
            RestorePageFromBlobOperation restorePageFromBlobOperation)
        {
            _queueSnapshotCache = queueSnapshotCache;
            _storage = storage;
            _queueSnapshotWriter = queueSnapshotWriter;
            _appGlobalFlags = appGlobalFlags;
            _appLogger = appLogger;
            _schedulerByTopic = schedulerByTopic;
            _restorePageFromBlobOperation = restorePageFromBlobOperation;
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
                Console.WriteLine("Making Empty Snapshot");
                await _storage.SaveAsync(Array.Empty<TopicAndQueuesSnapshotGrpcModel>());
                return Array.Empty<TopicAndQueuesSnapshotGrpcModel>();
            }
            
        }


        public async Task InitAsync()
        {

            const int snapshotId = 0;
            
            var fullSnapshot = await InitQueueSnapshot();
            
            _queueSnapshotCache.Init(fullSnapshot, snapshotId);
            _queueSnapshotWriter.Init(snapshotId);

            var tasks = new List<Task>();
            foreach (var topicAndQueuesSnapshot in fullSnapshot)
            {
                var pageId = MessagesContentPagesUtils.GetPageId(topicAndQueuesSnapshot.MessageId);

                var task = _schedulerByTopic.ExecuteTaskAsync(topicAndQueuesSnapshot.TopicId, pageId, "Init", async () =>
                {
                    await _restorePageFromBlobOperation.TryRestoreFromUncompressedPage(topicAndQueuesSnapshot.TopicId, pageId);
                });
                
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            _appGlobalFlags.Initialized = true;
            
            _appLogger.AddLog(LogProcess.System, null, "SYSTEM", "Application Initialized");
        }
  
    }
}