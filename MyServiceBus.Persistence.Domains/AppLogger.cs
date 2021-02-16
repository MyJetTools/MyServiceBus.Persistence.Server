using System;
using System.Collections;
using System.Collections.Generic;

namespace MyServiceBus.Persistence.Domains
{


    public interface IAppLogger
    {
        void AddLog(string context, string message, string stackTrace = null);
    }
    


    public class LogItem
    {
        public string Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Context { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
    
    
    public class AppLogger : IAppLogger
    {

        private readonly Dictionary<string, List<LogItem>> _logItems = new Dictionary<string, List<LogItem>>();

        private static void GcLogItems(IList logItems)
        {
            while (logItems.Count > 100)
            {
                logItems.RemoveAt(0);
            }
        }

        public void AddLog(string context, string message, string stackTrace)
        {
            
            
            var newItem = new LogItem
            {
                Context = context,
                Id = Guid.NewGuid().ToString("N"),
                Message = message,
                DateTime = DateTime.UtcNow,
                StackTrace = stackTrace
            };
            
                Console.WriteLine(newItem.DateTime.ToString("s")+": ["+context+"] "+message);
            
            
            lock (_logItems)
            {
                if (!_logItems.ContainsKey(context))
                    _logItems.Add(context, new List<LogItem>());

                var logItems = _logItems[context];
                logItems.Add(newItem);

                GcLogItems(logItems);

            }
            
        }
        
    }
}