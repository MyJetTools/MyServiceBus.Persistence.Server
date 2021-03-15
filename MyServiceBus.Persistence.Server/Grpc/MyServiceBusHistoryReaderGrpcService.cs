using System.Collections.Generic;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Server.Grpc
{
    public class MyServiceBusHistoryReaderGrpcService : IMyServiceBusHistoryReaderGrpcService
    {
        public async IAsyncEnumerable<MessageContentGrpcModel> GetByDateAsync(GetHistoryByDateGrpcRequest request)
        {
            using var requestHandler = ServiceLocator.CurrentRequests.StartRequest(request.TopicId, "GRPC History Request");
            
            await foreach (var message in ServiceLocator.MessagesContentReader.GetMessagesByDateAsync(
                request.TopicId, request.FromDateTime, requestHandler))
            {
                yield return message;
            }
        }
    }
}