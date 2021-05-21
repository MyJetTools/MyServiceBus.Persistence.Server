using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using MyServiceBus.Persistence.Domains;

namespace MyServiceBus.Persistence.Server.Controllers
{
    [ApiController]
    public class LogsController : Controller
    {


        private static string CompileLogs(IReadOnlyList<LogItem> logs, string header)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine(header);
            sb.AppendLine("------------------------------------------");
            foreach (var item in logs)
            {

                if (item.TopicId != null)
                {
                    sb.AppendLine(item.DateTime.ToString("O")+"; TopicId: "+item.TopicId);
                }
                else
                {
                    sb.AppendLine(item.DateTime.ToString("O"));
                }
                


                sb.AppendLine("Ctx:" + item.Context);

                sb.AppendLine("Message: " + item.Message);
                
                if (item.StackTrace != null)
                    sb.AppendLine("StackTrace: " + item.StackTrace);

                sb.AppendLine("----------------------");
            }

            return sb.ToString();
        }
        
        
        [HttpGet("/logs")]
        public IActionResult Logs()
        {
            var logs = ServiceLocator.AppLogger.Get(LogProcess.All);

            var content = CompileLogs(logs, $"Logs type: {LogProcess.All}");

            return Content(content);

        }
        
        
        [HttpGet("/logs/{logProcess}")]
        public IActionResult Logs(LogProcess logProcess)
        {
            var logs = ServiceLocator.AppLogger.Get(logProcess);

            var content = CompileLogs(logs, $"Logs type: {LogProcess.All}");

            return Content(content);
        }
        
        [HttpGet("/logs/topic/{topic}")]
        public IActionResult Logs(string topic)
        {
            var logs = ServiceLocator.AppLogger.GetByTopic(topic);

            var content = CompileLogs(logs, $"Logs by topic: {topic}");

            return Content(content);
        }
    }
}