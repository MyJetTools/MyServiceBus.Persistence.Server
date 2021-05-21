using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs
{


    public interface ITopicTask
    {
        string TopicId { get; }
        string Name { get; }
        
        bool Active { get; }
        
        DateTime Created { get; }
    }

    public class TopicTask : ITopicTask
    {
        public Func<Task> Callback { get; set; }
        
        public string TopicId { get; set; }
        
        public string Name { get; set; }
        public bool Active { get; set;}
        public DateTime Created { get; } = DateTime.UtcNow;

        public readonly TaskCompletionSource TaskCompletionSource = new ();
    }
    
    
    
    
    public class TaskSchedulerByTopic
    {
        private readonly IAppLogger _appLogger;

        private readonly Dictionary<string, Queue<TopicTask>> _tasksByTopics = new();
        private readonly Dictionary<string, Task> _systemTasks = new();

        private readonly Dictionary<string, TopicTask> _activeTasksByTopic = new();


        private readonly object _lockObject = new();

        public TaskSchedulerByTopic(IAppLogger appLogger)
        {
            _appLogger = appLogger;
        }


        public IReadOnlyList<ITopicTask> GetTasks()
        {
            List<ITopicTask> result = null;

            lock (_lockObject)
            {
                foreach (var task in _activeTasksByTopic.Values)
                {
                    result ??= new List<ITopicTask>();
                    result.Add(task);
                }

                foreach (var task in _tasksByTopics.Values.SelectMany(queue => queue))
                {
                    result ??= new List<ITopicTask>();
                    result.Add(task);
                }
            }

            return (IReadOnlyList<ITopicTask>) result ?? Array.Empty<ITopicTask>();

        }

        public (int pendingTasks, int activeTasks) GetTasksAmount()
        {
            lock (_lockObject)
            {
                var pending =  _tasksByTopics.Sum(itm => itm.Value.Count);
                var active = _activeTasksByTopic.Count;

                return (pending, active);
            }
        }

        public Task ExecuteTaskAsync(string topicId, string name, Func<Task> callback)
        {

            var task = new TopicTask
            {
                Name = name,
                Callback = callback,
                TopicId = topicId
            };

            lock (_lockObject)
            {
                if (!_tasksByTopics.ContainsKey(topicId))
                    _tasksByTopics.Add(topicId, new Queue<TopicTask>());
                
                _tasksByTopics[topicId].Enqueue(task);
                
                if (!_systemTasks.ContainsKey(topicId))
                    _systemTasks.Add(topicId, TopicTaskLoop(topicId));
            }


            return task.TaskCompletionSource.Task;

        }

        private TopicTask GetNextTopicTask(string topicId)
        {
            lock (_lockObject)
            {
                var result = _tasksByTopics[topicId].Count == 0 ? null : _tasksByTopics[topicId].Dequeue();

                if (result != null)
                {
                    _activeTasksByTopic.Add(topicId, result);
                }

                return result;
            }
        }


        private void RemoveFromActiveTasks(string topicId)
        {
            lock (_lockObject)
            {
                _activeTasksByTopic.Remove(topicId);
            }
        }

        private async Task TopicTaskLoop(string topicId)
        {
            while (true)
            {
                var nextTask = GetNextTopicTask(topicId);

                if (nextTask == null)
                {
                    await Task.Delay(200);
                    continue;
                }
                
                nextTask.Active = true;

                try
                {
                    await nextTask.Callback();
                    nextTask.TaskCompletionSource.SetResult();
                }
                catch (Exception e)
                {
                    _appLogger.AddLog(LogProcess.System, topicId, nextTask.Name ?? "Unknown Task", e.Message,
                        e.StackTrace);
                    nextTask.TaskCompletionSource.SetException(e);
                }
                finally
                {
                    RemoveFromActiveTasks(topicId);
                }
         
            }
        }



    }
}