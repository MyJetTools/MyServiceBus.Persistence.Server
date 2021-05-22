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
        
        public long MinMessageId { get; }
        public long MaxMessageId { get; }
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

        public static bool HasSkipped(this IMessageContentPage page)
        {
            var firstMessageId = page.PageId.Value * 100000;
            
            return page.MaxMessageId - firstMessageId >= page.Count;
        }

    }
    
}