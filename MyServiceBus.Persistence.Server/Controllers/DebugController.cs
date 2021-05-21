using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using MyServiceBus.Persistence.Domains.MessagesContent;

namespace MyServiceBus.Persistence.Server.Controllers
{
    [ApiController]
    public class DebugController : Controller
    {
        [HttpGet("/Debug/TestHoles")]
        public IActionResult TestHoles([Required][FromQuery]string topicId, [Required][FromQuery]long pageId)
        {
            var mPageId = new MessagePageId(pageId);
            var page = ServiceLocator.MessagesContentCache.TryGetPage(topicId, mPageId);

            if (page == null)
            {
                return Content("Topic not found");
            }

            var result = page.TestIfThereAreHoles(pageId);

            if (result.holes == null)
            {
                return Content("Everything is ok. Count: "+result.count);
            }

            var sb = new StringBuilder();

            sb.AppendLine("Count: " + result.count);

            foreach (var id in result.holes)
            {
                sb.AppendLine(id.ToString());
            }

            return Content(sb.ToString());
        }
    }
}