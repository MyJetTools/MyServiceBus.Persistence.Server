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
        public CompressedPage(ReadOnlyMemory<byte> content)
        {
            Content = content;
        }

        public CompressedPage(IReadOnlyList<MessageContentGrpcModel> page)
        {
            var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, page);
            stream.Position = 0;
            Content = stream.Zip();
        }

        public ReadOnlyMemory<byte> Content { get; private set; }

        public void EmptyIt()
        {
            Content = null;
        }

        public IReadOnlyList<MessageContentGrpcModel> UnCompress()
        {
            var unzippedMemory = Content.Unzip();
            return ProtoBuf.Serializer.Deserialize<List<MessageContentGrpcModel>>(unzippedMemory);
        }

        public static CompressedPage CreateEmpty()
        {
            return new ();
        }

        public ReadOnlyContentPage ToContentPage(MessagePageId pageId)
        {
            return Content.IsEmpty 
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