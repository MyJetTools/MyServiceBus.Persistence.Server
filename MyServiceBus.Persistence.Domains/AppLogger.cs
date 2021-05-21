using System;
using System.Collections.Generic;
using System.Linq;

namespace MyServiceBus.Persistence.Domains
{


    public interface IAppLogger
    {
        void AddLog(string context, string message, string stackTrace = null);

        IReadOnlyList<LogItem> Get();
    }
    


    public class LogItem
    {
        public DateTime DateTime { get; set; }
        public string Context { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
    
    
    public class AppLogger : IAppLogger
    {

        private readonly Queue<LogItem> _logItems = new ();

        private void GcLogItems()
        {
            while (_logItems.Count > 100)
            {
                _logItems.Dequeue();
            }
        }

        public void AddLog(string context, string message, string stackTrace)
        {


            var newItem = new LogItem
            {
                Context = context,
                Message = message,
                DateTime = DateTime.UtcNow,
                StackTrace = stackTrace
            };

            Console.WriteLine(newItem.DateTime.ToString("s") + ": [" + context + "] " + message);

            lock (_logItems)
            {
                _logItems.Enqueue(newItem);
                GcLogItems();
            }

        }

        public IReadOnlyList<LogItem> Get()
        {
            lock (_logItems)
            {
                return _logItems.ToList();
            }
        }
        
    }
}