using System;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations;

namespace MyServiceBus.Persistence.Domains
{
    public class ServicesDisposer
    {

        private readonly PersistentOperationsScheduler _persistentOperationsScheduler;
        private readonly QueueSnapshotWriter _queueSnapshotWriter;

        public ServicesDisposer(PersistentOperationsScheduler persistentOperationsScheduler, 
            QueueSnapshotWriter queueSnapshotWriter)
        {
            _persistentOperationsScheduler = persistentOperationsScheduler;
            _queueSnapshotWriter = queueSnapshotWriter;
        }

        public async Task Shutdown()
        {
            await Task.Delay(300);
            
            var hasOperationsToFinish = true;
            
            while (hasOperationsToFinish)
            {
                hasOperationsToFinish = false;

                Console.WriteLine("Persistent Queue size is: "+ _persistentOperationsScheduler.GetQueueSize());
                Console.WriteLine("Active Operations Are: "+ _persistentOperationsScheduler.GetActiveOperations().Count);
                
                if (_persistentOperationsScheduler.HasOperationsToExecute())
                {
                    hasOperationsToFinish = true;
                    await _persistentOperationsScheduler.ExecuteOperationAsync();
                }

                await Task.Delay(500);

            }
            
            Console.WriteLine("Persistent Queue size is: "+ _persistentOperationsScheduler.GetQueueSize());

            
            await _queueSnapshotWriter.ExecuteAsync();
        }
        
    }
}