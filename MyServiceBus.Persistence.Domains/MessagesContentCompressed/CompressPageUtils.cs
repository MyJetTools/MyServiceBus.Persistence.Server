using System;
using System.Collections.Generic;
using System.IO;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContentCompressed
{


    public static class CompressPageUtils
    {

        public static ReadOnlyMemory<byte> ZipMessages(this IReadOnlyList<MessageContentGrpcModel> messages)
        {

            var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, messages);
            stream.Position = 0;
            return stream.Zip();
        }


        public static IReadOnlyList<MessageContentGrpcModel> UnzipMessages(this ReadOnlyMemory<byte> zippedContent)
        {
            if (zippedContent.IsEmpty)
            {
                return Array.Empty<MessageContentGrpcModel>();
            }

            var unzippedMemory = zippedContent.Unzip();
            return ProtoBuf.Serializer.Deserialize<List<MessageContentGrpcModel>>(unzippedMemory) as
                IReadOnlyList<MessageContentGrpcModel> ?? Array.Empty<MessageContentGrpcModel>();
        }

    }


    
    public class CompressedPage 
    {
        
        public CompressedPage(MessagePageId pageId, ReadOnlyMemory<byte> zippedContent)
        {
            ZippedContent = zippedContent;
            Messages = zippedContent.UnzipMessages();
            PageId = pageId;
        }

        public CompressedPage(MessagePageId pageId, IReadOnlyList<MessageContentGrpcModel> messages)
        {
            ZippedContent = messages.ZipMessages();
            Messages = messages;
            PageId = pageId;
        }

        
        public MessagePageId PageId { get; }
        public ReadOnlyMemory<byte> ZippedContent { get; private set; }

        public void EmptyIt()
        {
            ZippedContent = null;
            Messages = Array.Empty<MessageContentGrpcModel>();
        }
        
        public IReadOnlyList<MessageContentGrpcModel> Messages { get; private set; }


        public static CompressedPage CreateEmpty(MessagePageId pageId)
        {
            return new (pageId, ReadOnlyMemory<byte>.Empty);
        }


        public ReadOnlyContentPage ToReadOnlyContentPage()
        {
            return new (this);
        }

    }

    

}