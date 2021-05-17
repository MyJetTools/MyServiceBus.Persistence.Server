using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Server.Grpc
{
    public class MyServiceBusMessagesPersistenceGrpcService : IMyServiceBusMessagesPersistenceGrpcService
    {
        public async IAsyncEnumerable<byte[]> GetPageCompressedAsync(GetMessagesPageGrpcRequest request)
        {
            if (!ServiceLocator.AppGlobalFlags.Initialized)
                throw new Exception("App is not initialized yet");

            using var requestHandler = ServiceLocator.CurrentRequests.StartRequest(request.TopicId,
                "GRPC GetPageCompressed");
            
            var messagePageId = new MessagePageId(request.PageNo);

            var page = await ServiceLocator.MessagesContentReader.TryGetPageAsync(request.TopicId,
                messagePageId, requestHandler);

            if (page == null) 
                yield break;
            
            foreach (var batch in page.GetCompressedPage().Content.BatchIt(1024*1024*3))
                yield return batch.ToArray();
        }

        public async ValueTask SaveMessagesAsync(IAsyncEnumerable<byte[]> request)
        {
            if (!ServiceLocator.AppGlobalFlags.Initialized)
                throw new Exception("App is not initialized yet");

            if (ServiceLocator.AppGlobalFlags.IsShuttingDown)
                throw new Exception("App is stopping");

            var contract = await request.DecompressAndMerge<SaveMessagesGrpcContract>();

            if (contract.Messages == null)
            {
                Console.WriteLine(contract.TopicId+": Request to Save messages with empty content");
                return;
            }
            
            ServiceLocator.IndexByMinuteWriter.NewMessages(contract.TopicId, contract.Messages);

            var topicDataLocator = ServiceLocator.TopicsList.GetOrCreate(contract.TopicId);
            
            var groups = contract.Messages
                .GroupBy(itm => MessagesContentPagesUtils.GetPageId(itm.MessageId).Value);
            
            foreach (var group in groups)
            {
                var messagePageId = new MessagePageId(group.Key);
                
                var writablePage = topicDataLocator.GetOrCreateWritablePage(messagePageId);
                writablePage.Add(group);
                
                topicDataLocator.MessagesToPersist.Append(messagePageId, group);
            }
        }

        public async ValueTask<MessageContentGrpcModel> GetMessageAsync(GetMessageGrpcRequest request)
        {
            if (!ServiceLocator.AppGlobalFlags.Initialized)
                throw new Exception("App is not initialized yet");

            using var requestHandler = ServiceLocator.CurrentRequests.StartRequest(request.TopicId, "GRPC GetMessage");
            
            var (result, _) 
                = await ServiceLocator.MessagesContentReader.TryGetMessageAsync(request.TopicId, request.MessageId, requestHandler);

            return result;
        }

    }
}