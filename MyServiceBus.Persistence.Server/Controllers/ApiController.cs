using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
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
                        pageId = itm.MessagePageId.Value,
                        dur = (now - itm.Created).ToString()
                    }),

                awaitingOperations = tasks.Where(itm => !itm.Active)
                    .Select(itm => new
                    {
                        topicId = itm.TopicId,
                        name = itm.Name,
                        pageId = itm.MessagePageId.Value,
                        dur = (now - itm.Created).ToString()
                    }),

                queuesSnapshotId = messageSnapshot.SnapshotId,

                topics = ServiceLocator.MessagesContentCache
                    .GetLoadedPages()
                    .Select(itm =>
                {

                    var snapshot = messageSnapshot.Cache.FirstOrDefault(st => st.TopicId == itm.Key);
                    
                    

                    var topicMetrics = ServiceLocator.MetricsByTopic.Get(itm.Key);

                    return new
                    {
                        topicId = itm.Key,
 
                        messageId = snapshot?.MessageId ?? -1,
                        savedMessageId = topicMetrics.MaxSavedMessageId,
                        lastSaveChunk = topicMetrics.LastSavedChunk,
                        lastSaveDur = topicMetrics.LastSaveDuration.ToString(),
                        lastSaveMoment =  (DateTime.UtcNow - topicMetrics.LastSaveMoment).ToString(),
                        loadedPages = itm.Value.OrderBy(page => page.PageId.Value)
                            .Select(page =>
                            {
                                var writePosition =
                                    ServiceLocator.MessagesContentPersistentStorage.GetWritePosition(itm.Key,
                                        page.PageId);
                                return new
                                {
                                    pageId = page.PageId.Value,
                                    hasSkipped = page.HasSkipped(),
                                    percent = page.Percent(),
                                    count = page.Count,
                                    writePosition = writePosition,
                                };
                            }),
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