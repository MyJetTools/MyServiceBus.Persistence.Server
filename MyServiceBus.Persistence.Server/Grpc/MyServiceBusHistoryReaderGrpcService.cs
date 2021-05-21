using System;
using System.Collections.Generic;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Server.Grpc
{
    public class MyServiceBusHistoryReaderGrpcService : IMyServiceBusHistoryReaderGrpcService
    {
        public async IAsyncEnumerable<MessageContentGrpcModel> GetByDateAsync(GetHistoryByDateGrpcRequest request)
        {
            if (!ServiceLocator.AppGlobalFlags.Initialized)
                throw new Exception("App is not initialized yet");
            
            await foreach (var message in  ServiceLocator.MessagesContentReader.GetMessagesByDateAsync(request.TopicId, 
                request.FromDateTime, "GRPC History Request"))
            {
                yield return message;
            }
        }
    }
}