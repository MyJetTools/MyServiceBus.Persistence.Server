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
        
        bool HasSkippedId { get; }

    }



    public static class MessageContentPageExtensions
    {


        public static CompressedPage GetCompressedPage(this IMessageContentPage page)
        {
            return new (page.PageId, page.GetMessages());
        }


        public static int Percent(this IMessageContentPage page)
        {
            return page.Count / 1000;
        }
    }
    
}