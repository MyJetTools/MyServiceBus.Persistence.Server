using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MyServiceBus.Persistence.AzureStorage.TopicMessages;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Server.Controllers
{
    
    [ApiController]
    public class ApiController : Controller
    {
        [HttpGet("api/isalive")]
        public IActionResult Index()
        {
            return Content("Alive");
        }


        [HttpGet("api/status")]
        public IActionResult Status()
        {

            var messageSnapshot = ServiceLocator.QueueSnapshotCache.Get();

            var tasks = ServiceLocator.TaskSchedulerByTopic.GetTasks();

            var now = DateTime.UtcNow;
            var resultObject = new
            {
                
                activeOperations = tasks.Where(itm => itm.Active)
                    .Select(itm => new
                {
                    topicId = itm.TopicId,
                    name = itm.Name,
                    dur = (now - itm.Created).ToString()
                }),
                
                awaitingOperations = tasks.Where(itm => !itm.Active)
                    .Select(itm => new
                    {
                        topicId = itm.TopicId,
                        name = itm.Name,
                        dur = (now - itm.Created).ToString()
                    }),
                
                queuesSnapshotId = messageSnapshot.SnapshotId,
           
                LoadedPages = ServiceLocator.MessagesContentCache.GetLoadedPages().Select(itm =>
                {

                    var snapshot = messageSnapshot.Cache.FirstOrDefault(st => st.TopicId == itm.Key);
                    
                    return new
                    {
                        topicId = itm.Key,
                        writePosition = PageWriter.GetWritePosition(itm.Key),
                        messageId = snapshot?.MessageId ?? -1,
                        savedMessageId = ServiceLocator.MaxPersistedMessageIdByTopic.GetOrDefault(itm.Key),
                        pages = itm.Value.OrderBy(pageId => pageId),
                        activePages = (snapshot?.GetActivePages()
                                .Select(messagePageId => messagePageId.Value) ?? Array.Empty<long>())
                            .OrderBy(pageId => pageId), 
                        queues = snapshot != null ? snapshot.QueueSnapshots : Array.Empty<QueueSnapshotGrpcModel>()
                    };
                })
            };
            return Json(resultObject);
        }

    }
    
    
    
}