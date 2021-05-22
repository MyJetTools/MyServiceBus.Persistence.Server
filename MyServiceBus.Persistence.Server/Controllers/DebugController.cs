using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.Server.Controllers
{
    [ApiController]
    public class DebugController : Controller
    {
        [HttpGet("/Debug/Page")]
        public async ValueTask<IActionResult> Index([Required][FromQuery]string topicId, [Required][FromQuery]long pageId)
        {

            var messagePageId = new MessagePageId(pageId);
            var page = ServiceLocator.MessagesContentCache.TryGetPage(topicId, messagePageId);

            if (page == null)
            {
                await ServiceLocator.TaskSchedulerByTopic.ExecuteTaskAsync(topicId, messagePageId, "Load Debug Page",
                    async () =>
                    {
                        await ServiceLocator.MessagesContentReader.LoadPageIntoCacheTopicSynchronizedAsync(topicId,
                            messagePageId);

                    });
            }
            
            page = ServiceLocator.MessagesContentCache.TryGetPage(topicId, messagePageId);

            if (page == null)
                return NotFound("Page Not Found");


            var result = new
            {
                page.MinMessageId,
                page.MaxMessageId,
                page.Count,
                shouldHaveAmount = page.ShouldHaveAmount(),
                hasSkipped = page.HasSkipped()
            };


            return Json(result);

        }
    }
}