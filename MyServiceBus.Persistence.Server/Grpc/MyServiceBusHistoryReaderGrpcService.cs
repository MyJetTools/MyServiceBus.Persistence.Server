using System.Collections.Generic;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Server.Grpc
{
    public class MyServiceBusHistoryReaderGrpcService : IMyServiceBusHistoryReaderGrpcService
    {
        public async IAsyncEnumerable<MessageContentGrpcModel> GetByDateAsync(GetHistoryByDateGrpcRequest request)
        {
            await foreach (var message in  ServiceLocator.MessagesContentReader.GetMessagesByDateAsync(request.TopicId, 
                request.FromDateTime, "GRPC History Request"))
            {
                yield return message;
            }
        }
    }
}