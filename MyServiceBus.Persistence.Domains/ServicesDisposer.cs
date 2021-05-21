using System;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.BackgroundJobs;

namespace MyServiceBus.Persistence.Domains
{
    public class ServicesDisposer
    {
        private readonly TaskSchedulerByTopic _taskSchedulerByTopic;
        private readonly QueueSnapshotWriter _queueSnapshotWriter;

        public ServicesDisposer(TaskSchedulerByTopic taskSchedulerByTopic, 
            QueueSnapshotWriter queueSnapshotWriter)
        {
            _taskSchedulerByTopic = taskSchedulerByTopic;
            _queueSnapshotWriter = queueSnapshotWriter;
        }

        public async Task Shutdown()
        {
            await Task.Delay(500);
            
            var hasOperationsToFinish = true;
            
            while (hasOperationsToFinish)
            {
                var tasks = _taskSchedulerByTopic.GetTasksAmount();

                Console.WriteLine("Pending tasks are: "+ tasks.pendingTasks);
                Console.WriteLine("Active tasks are: "+ tasks.activeTasks);

                await Task.Delay(500);
                
                hasOperationsToFinish = tasks.pendingTasks > 0 || tasks.activeTasks > 0;
            }
            
            Console.WriteLine("All tasks are finished");
            
            await _queueSnapshotWriter.ExecuteAsync();
        }
        
    }
}