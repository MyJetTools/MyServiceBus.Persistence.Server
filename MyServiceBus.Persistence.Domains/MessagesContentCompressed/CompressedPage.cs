using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContentCompressed
{
    public struct CompressedPage
    {
        public CompressedPage(ReadOnlyMemory<byte> zippedContent)
        {
            ZippedContent = zippedContent;

            if (zippedContent.IsEmpty)
            {
                Messages = Array.Empty<MessageContentGrpcModel>();
            }
            else
            {
                var unzippedMemory = ZippedContent.Unzip();
                Messages = ProtoBuf.Serializer.Deserialize<List<MessageContentGrpcModel>>(unzippedMemory);   
            }
            

        }

        public CompressedPage(IReadOnlyList<MessageContentGrpcModel> page)
        {
            Messages = page;
            var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, page);
            stream.Position = 0;
            ZippedContent = stream.Zip();
        }

        public ReadOnlyMemory<byte> ZippedContent { get; private set; }

        public void EmptyIt()
        {
            ZippedContent = null;
            Messages = Array.Empty<MessageContentGrpcModel>();
        }
        
        public IReadOnlyList<MessageContentGrpcModel> Messages { get; private set; }


        public static CompressedPage CreateEmpty()
        {
            return new ();
        }

        public ReadOnlyContentPage ToContentPage(MessagePageId pageId)
        {
            return ZippedContent.IsEmpty 
                ? null 
                : new ReadOnlyContentPage(pageId, this);
        }
    }


    public static class CompressedPageUtils
    {
        public static async ValueTask<ReadOnlyContentPage> ToContentPageAsync(this Task<CompressedPage> contentPageTask, MessagePageId pageId)
        {
            var contentPage = await contentPageTask;
            return contentPage.ToContentPage(pageId);
        }
    }
    
    

}