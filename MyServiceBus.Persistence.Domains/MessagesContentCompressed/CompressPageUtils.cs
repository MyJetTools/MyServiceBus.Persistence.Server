using System;
using System.Collections.Generic;
using System.IO;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Grpc;
using MyServiceBus.MessagesCompressor;


namespace MyServiceBus.Persistence.Domains.MessagesContentCompressed
{


    public static class CompressPageUtils
    {

        public static ReadOnlyMemory<byte> ZipMessages(this IReadOnlyList<MessageContentGrpcModel> messages)
        {

            var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, messages);
            stream.Position = 0;
            return stream.Compress();
        }


        public static IReadOnlyList<MessageContentGrpcModel> UnzipMessages(this ReadOnlyMemory<byte> zippedContent)
        {
            if (zippedContent.IsEmpty)
            {
                return Array.Empty<MessageContentGrpcModel>();
            }

            var unzippedMemory = zippedContent.Decompress();
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
            CalcMinMax();
        }

        public CompressedPage(MessagePageId pageId, IReadOnlyList<MessageContentGrpcModel> messages)
        {
            ZippedContent = messages.ZipMessages();
            Messages = messages;
            PageId = pageId;
            CalcMinMax();
        }

        private void CalcMinMax()
        {

    
            
            foreach (var msg in Messages)
            {
                if (MinMessagesId < 0)
                {
                    MinMessagesId = msg.MessageId;
                }
                else
                {
                    if (MinMessagesId > msg.MessageId)
                    {
                        MinMessagesId = msg.MessageId;
                    }
                }
                
                if (MaxMessagesId < 0)
                {
                    MaxMessagesId = msg.MessageId;
                }
                else
                {
                    if (MaxMessagesId < msg.MessageId)
                    {
                        MaxMessagesId = msg.MessageId;
                    }
                }
                
            }
        }

        
        public MessagePageId PageId { get; }
        public ReadOnlyMemory<byte> ZippedContent { get; }


        public IReadOnlyList<MessageContentGrpcModel> Messages { get; }


        public static CompressedPage CreateEmpty(MessagePageId pageId)
        {
            return new (pageId, ReadOnlyMemory<byte>.Empty);
        }


        public ReadOnlyContentPage ToReadOnlyContentPage()
        {
            return new (this);
        }

        public long MinMessagesId { get; private set; } = -1;
        public long MaxMessagesId { get; private set; } = -2;

    }

    

}