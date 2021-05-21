using System;
using System.Collections.Generic;
using System.Linq;

namespace MyServiceBus.Persistence.Domains
{


    public interface IAppLogger
    {
        void AddLog(LogProcess logProcess, string topicId, string context, string message, string stackTrace = null);

        IReadOnlyList<LogItem> Get(LogProcess logProcess);
        
        IReadOnlyList<LogItem> GetByTopic(string topicId);
    }

    public class LogItem 
    {
        public DateTime DateTime { get; set; }
        public string Context { get; set; }
        public string TopicId { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }

    public enum LogProcess
    {
        All, System, PagesCompressor, PagesLoaderOrGc, MinuteIndexWriter
    }
    
    
    public class AppLogger : IAppLogger
    {

        private readonly Dictionary<LogProcess, Queue<LogItem>> _logItems = new ();
        
        private readonly Dictionary<string, Queue<LogItem>> _logItemsByTopic = new ();

        public AppLogger()
        {
            foreach (var logProcess in Enum.GetValues<LogProcess>())
            {
                _logItems.Add(logProcess, new Queue<LogItem>());   
            }
        }

        private static void GcLogItems(Queue<LogItem> logItems)
        {
            while (logItems.Count > 100)
            {
                logItems.Dequeue();
            }
        }

        public void AddLog(LogProcess logProcess, string topicId, string context, string message, string stackTrace)
        {
            var newItem = new LogItem
            {
                Context = context,
                Message = message,
                DateTime = DateTime.UtcNow,
                StackTrace = stackTrace,
                TopicId = topicId
            };

            Console.WriteLine(newItem.DateTime.ToString("s") + ": [" + context + "] " + message);

            lock (_logItems)
            {

                var logItems = _logItems[logProcess];
                logItems.Enqueue(newItem);
                GcLogItems(logItems);

                logItems = _logItems[LogProcess.All];
                logItems.Enqueue(newItem);
                GcLogItems(logItems);

                if (topicId != null)
                {
                    
                    if (!_logItemsByTopic.ContainsKey(topicId))
                        _logItemsByTopic.Add(topicId, new Queue<LogItem>());

                    var logItemsByTopic = _logItemsByTopic[topicId];
                    
                    logItemsByTopic.Enqueue(newItem);
                    GcLogItems(logItemsByTopic);

                }
            }

        }

        public IReadOnlyList<LogItem> Get(LogProcess logProcess)
        {
            lock (_logItems)
            {
                return  _logItems[logProcess].ToList();
            }
        }

        public IReadOnlyList<LogItem> GetByTopic(string topicId)
        {
            lock (_logItems)
            {
                if (_logItemsByTopic.TryGetValue(topicId, out var result))
                    return result.ToList();
                
                
                return Array.Empty<LogItem>();
            }
        }
    }
}