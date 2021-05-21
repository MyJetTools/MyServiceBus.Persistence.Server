using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Server.Grpc
{
    public class MyServiceBusMessagesPersistenceGrpcService : IMyServiceBusMessagesPersistenceGrpcService
    {

        public async IAsyncEnumerable<byte[]> GetPageCompressedAsync(GetMessagesPageGrpcRequest request)
        {
            if (!ServiceLocator.AppGlobalFlags.Initialized)
                throw new Exception("App is not initialized yet");

            var page = await ServiceLocator.MessagesContentReader.TryGetPageAsync(request.TopicId,
                new MessagePageId(request.PageNo), "GRPC Request");

            if (page != null)
            {
                foreach (var batch in page.GetCompressedPage().ZippedContent.BatchIt(1024*1024*3))
                    yield return batch.ToArray();
            }
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
            
            var groups = contract.Messages
                .GroupBy(itm => MessagesContentPagesUtils.GetPageId(itm.MessageId).Value);
            
            foreach (var group in groups)
            {
                var messagePageId = new MessagePageId(group.Key);
                ServiceLocator.PersistentOperationsScheduler.WriteMessagesAsync(contract.TopicId, "GRPC Request", messagePageId, group);
                var page = ServiceLocator.MessagesContentCache.GetOrCreateWritablePage(contract.TopicId, messagePageId);
                page.Add(group);
            }

        }


        public async ValueTask<MessageContentGrpcModel> GetMessageAsync(GetMessageGrpcRequest request)
        {
            if (!ServiceLocator.AppGlobalFlags.Initialized)
                throw new Exception("App is not initialized yet");

            var (result, _) = await ServiceLocator.MessagesContentReader.TryGetMessageAsync(request.TopicId, request.MessageId,
                "GRPC Request");

            return result;
        }


    }
}