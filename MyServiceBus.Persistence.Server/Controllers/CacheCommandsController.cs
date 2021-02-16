using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyServiceBus.Persistence.AzureStorage.CompressedMessages;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Server.Models;

namespace MyServiceBus.Persistence.Server.Controllers
{
    [ApiController]
    public class CacheCommandsController : Controller
    {
        [HttpPost("[Controller]/Compress")]
        public async Task<CompressPageResult> CompressAsync([FromForm]string topicId, [FromForm]long pageId)
        {

            var snapshot = ServiceLocator.QueueSnapshotCache.Get();

            var topicSnapshot = snapshot.Cache.FirstOrDefault(itm => itm.TopicId == topicId);

            if (topicSnapshot == null)
                return new CompressPageResult
                {
                    Result = "Topic not found"
                };


            var messagePageId = new MessagePageId(pageId);
            
            if (!ServiceLocator.CompressedMessagesUtils.PageCanBeCompressed(topicId, messagePageId))
            {
                return new CompressPageResult
                {
                    Result = "Can not compress current page"
                };
            }

            var page = await ServiceLocator.MessagesContentReader.TryGetPageAsync(topicId, messagePageId, "API-Request");

            await ServiceLocator.PersistentOperationsScheduler.CompressPageAsync(topicId, page, "API request");

            return new CompressPageResult
            {
                Result = "Page is compressed"
            };
        }
        
        [HttpGet("[Controller]/Message")]
        public async Task<MessageContentModel> GetMessageAsync([FromQuery]string topicId, [FromQuery]long messageId)
        {
            var (message, page) = await ServiceLocator.MessagesContentReader.TryGetMessageAsync(topicId, messageId, "API Request");

            if (message == null)
                return new MessageContentModel
                {
                    Id = -1,
                };

            
            return new MessageContentModel
            {
                Id = message.MessageId,
                Content = message.Data == null ? null : Convert.ToBase64String(message.Data),
                Created = message.Created,
                PageSize = page.Count
            };
        }
        
        [HttpGet("[Controller]/MessageFromLegacy")]
        public async Task<MessageContentModel> GetMessageFromLegacyAsync([FromQuery]string topicId, [FromQuery]long messageId)
        {
            var pageId = MessagesContentPagesUtils.GetPageId(messageId);
            var compressedPage = await ServiceLocator.LegacyCompressedMessagesStorage.GetCompressedPageAsync(topicId, pageId);
            
            if (compressedPage.Content.IsEmpty)
                return new MessageContentModel
                {
                    Id = -1,
                };


            var page = compressedPage.ToContentPage(pageId);

            var message = page.TryGet(messageId);

            foreach (var grpcModel in page.GetMessages())
            {
                var messagePageId = MessagesContentPagesUtils.GetPageId(grpcModel.MessageId);

                if (messagePageId.Value != pageId.Value)
                    Console.WriteLine(grpcModel.MessageId+" does not belong to the page: "+pageId.Value);
            }
            

            return new MessageContentModel
            {
                Id = message.MessageId,
                Content = message.Data == null ? null : Convert.ToBase64String(message.Data),
                Created = message.Created,
                PageSize = page.Count
            };
        }

        [HttpGet("[Controller]/ConvertToNewPageCluster")]
        public async Task ConvertToNewPageCluster([FromQuery]string topicId, [FromQuery]long clusterPageId)
        {
            foreach (var pageId in CompressedMessagesStorageUtils.GetMessagePages(new ClusterPageId(clusterPageId)))
            {
                Console.WriteLine("Reading: "+topicId+"/"+pageId.Value);
                var pageData = await ServiceLocator.LegacyCompressedMessagesStorage.GetCompressedPageAsync(topicId, pageId);
                
                if (!pageData.Content.IsEmpty)
                {
                    Console.WriteLine("Writing: "+topicId+"/"+pageId.Value);
                    await ServiceLocator.CompressedMessagesStorage.WriteCompressedPageAsync(topicId, pageId, pageData);
                }
            }
        }

    }
}