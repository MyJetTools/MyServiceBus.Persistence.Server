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
        
        public LogProcess LogProcess { get; set; }

        public string Id { get; } = Guid.NewGuid().ToString("N");
        public string Context { get; set; }
        public string TopicId { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }

    public enum LogProcess
    {
        All, System, PagesCompressor, PagesLoaderOrGc, MinuteIndexWriter, NewMessages, Debug
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


        private void InsertItem(LogItem newItem)
        {
            var logItems = _logItems[newItem.LogProcess];
            logItems.Enqueue(newItem);
            GcLogItems(logItems);

            logItems = _logItems[LogProcess.All];
            logItems.Enqueue(newItem);
            GcLogItems(logItems);

            if (newItem.TopicId != null)
            {
                    
                if (!_logItemsByTopic.ContainsKey(newItem.TopicId))
                    _logItemsByTopic.Add(newItem.TopicId, new Queue<LogItem>());

                var logItemsByTopic = _logItemsByTopic[newItem.TopicId];
                    
                logItemsByTopic.Enqueue(newItem);
                GcLogItems(logItemsByTopic);

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
                TopicId = topicId,
                LogProcess = logProcess
            };

            

            lock (_logItems)
            {
                
                if (topicId == null)
                {
                    Console.WriteLine(newItem.DateTime.ToString("s") + ": [" + context + "] " + message);    
                }
                else
                {
                    Console.WriteLine(newItem.DateTime.ToString("s") + ": Topic: "+topicId+"; [" + context + "] " + message);
                }
                
                if (newItem.StackTrace != null)
                    Console.WriteLine("StackTrace: "+newItem.StackTrace);
                
                InsertItem(newItem);
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


        public IReadOnlyList<LogItem> GetSnapshot()
        {
            var result = new Dictionary<string, LogItem>();

            lock (_logItems)
            {
                foreach (var logItem in from logs in _logItems.Values from logItem in logs where !result.ContainsKey(logItem.Id) select logItem)
                {
                    result.Add(logItem.Id, logItem);
                }
                
                foreach (var logItem in from logs in _logItemsByTopic.Values from logItem in logs where !result.ContainsKey(logItem.Id) select logItem)
                {
                    result.Add(logItem.Id, logItem);
                }
            }

            return result.Values.OrderBy(itm => itm.DateTime).ToList();
        }

        public void Init(IEnumerable<LogItem> items)
        {

            lock (_logItems)
            {
                foreach (var item in items)
                {
                    InsertItem(item);
                }
            }
            
        }
    }
}