using System;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations;
using MyServiceBus.Persistence.Domains.ExecutionProgress;

namespace MyServiceBus.Persistence.Domains
{
    public class ServicesDisposer
    {
        private readonly CurrentRequests _currentRequests;
        private readonly QueueSnapshotWriter _queueSnapshotWriter;

        public ServicesDisposer(CurrentRequests currentRequests,
            QueueSnapshotWriter queueSnapshotWriter)
        {
            _currentRequests = currentRequests;
            _queueSnapshotWriter = queueSnapshotWriter;
        }

        public async Task Shutdown()
        {
            await Task.Delay(300);


            while (_currentRequests.RequestsAmount > 0)
            {
                Console.WriteLine(
                    $"Long running requests in progress are {_currentRequests.RequestsAmount}. Waiting for 500ms...");

                await Task.Delay(500);
            }

            Console.WriteLine($"Long running requests in progress are {_currentRequests.RequestsAmount}. We stopped Ok here.");
            
            await _queueSnapshotWriter.ExecuteAsync();
        }
        
    }
}