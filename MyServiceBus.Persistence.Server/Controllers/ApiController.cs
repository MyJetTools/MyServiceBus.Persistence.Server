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
            
            var resultObject = new
            {
                
                activeOperations = ServiceLocator.PersistentOperationsScheduler
                    .GetActiveOperations()
                    .Select(itm => new
                {
                    id = itm.Id,
                    name = itm.OperationFriendlyName,
                    pageId = itm.PageId.Value,
                    topicId = itm.TopicId,
                    reason = itm.Reason
                }),
                
                awaitingOperations = ServiceLocator.PersistentOperationsScheduler
                    .GetAwaiting()
                    .Select(itm => new
                {
                    id = itm.Id,
                    name = itm.OperationFriendlyName,
                    pageId = itm.PageId.Value,
                    topicId = itm.TopicId,
                    reason = itm.Reason
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