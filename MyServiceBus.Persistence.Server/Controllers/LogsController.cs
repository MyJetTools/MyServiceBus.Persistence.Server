using System.Text;
using Microsoft.AspNetCore.Mvc;
using MyServiceBus.Persistence.Domains;

namespace MyServiceBus.Persistence.Server.Controllers
{
    [ApiController]
    public class LogsController : Controller
    {
        
        
        [HttpGet("/logs")]
        public IActionResult Logs()
        {
            var logs = ServiceLocator.AppLogger.Get(LogProcess.All);

            var sb = new StringBuilder();
            
            sb.AppendLine($"Logs type: {LogProcess.All}");
            sb.AppendLine("------------------------------------------");
            foreach (var item in logs)
            {
                sb.AppendLine(item.DateTime.ToString("O") + $" Ctx: {item.Context}");
                sb.AppendLine("Message: " + item.Message);
                
                if (item.StackTrace != null)
                    sb.AppendLine("StackTrace: " + item.StackTrace);

                sb.AppendLine("----------------------");
            }

            return Content(sb.ToString());

        }
        
        
        [HttpGet("/logs/{logProcess}")]
        public IActionResult Logs(LogProcess logProcess)
        {
            var logs = ServiceLocator.AppLogger.Get(logProcess);

            var sb = new StringBuilder();

            sb.AppendLine($"Logs type: {logProcess}");
            sb.AppendLine("------------------------------------------");
            foreach (var item in logs)
            {
                sb.AppendLine(item.DateTime.ToString("O") + $" Ctx: {item.Context}");
                sb.AppendLine("Message: " + item.Message);
                
                if (item.StackTrace != null)
                    sb.AppendLine("StackTrace: " + item.StackTrace);

                sb.AppendLine("----------------------");
            }

            return Content(sb.ToString());

        }
    }
}