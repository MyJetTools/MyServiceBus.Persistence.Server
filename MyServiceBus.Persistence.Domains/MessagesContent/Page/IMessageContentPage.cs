using System;
using System.Collections.Generic;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent.Page
{
    public interface IMessageContentPage
    {
        
        MessagePageId PageId { get; }
        MessageContentGrpcModel TryGet(long messageId);
        CompressedPage GetCompressedPage();
        
        DateTime LastAccessTime { get;}
        
        int Count { get; }

        IReadOnlyList<MessageContentGrpcModel> GetMessages();

    }



    public static class MessageContentPageExtensions
    {
        public static bool HasAllMessages(this IMessageContentPage page)
        {
            return page.Count == 100000;
        }
    }
    
}