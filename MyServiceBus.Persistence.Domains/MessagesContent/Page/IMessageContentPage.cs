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
        
        DateTime LastAccessTime { get;}
        
        int Count { get; }
        
        long TotalContentSize { get; }

        IReadOnlyList<MessageContentGrpcModel> GetMessages();

    }



    public static class MessageContentPageExtensions
    {
        public static bool HasAllMessages(this IMessageContentPage page)
        {
            return page.Count == 100000;
        }


        public static CompressedPage GetCompressedPage(this IMessageContentPage page)
        {
            return new (page.PageId, page.GetMessages());
        }

    }
    
}