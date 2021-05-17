using System;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Server.Models
{
    public class MessageRestApiModel
    {
        public long Id { get; set; }
        public DateTime Created { get; set; }
        public string Content { get; set; }

        public static MessageRestApiModel Create(MessageContentGrpcModel model)
        {
            return new()
            {
                Id = model.MessageId,
                Content = Convert.ToBase64String(model.Data),
                Created = model.Created
            };
        }
        
    }
}