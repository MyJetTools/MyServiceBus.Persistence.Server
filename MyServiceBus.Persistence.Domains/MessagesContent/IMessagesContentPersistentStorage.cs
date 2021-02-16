using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent
{

    public interface IPageWriter
    {
        Task WriteAsync (IEnumerable<MessageContentGrpcModel> messagesToWrite);

        Task WaitUntilInitializedAsync();

        IMessageContentPage GetAssignedPage();

        Task AssignPageAndInitialize(WritableContentCachePage page, AppGlobalFlags appGlobalFlags);

        bool Initialized { get; }
        

    }
    
    public interface IMessagesContentPersistentStorage
    {

        Task DeleteNonCompressedPageAsync(string topicId, MessagePageId pageId);
        
        Task<IPageWriter> CreatePageWriterAsync(string topicId, MessagePageId pageId, bool cleanIt, WritableContentCachePage page, AppGlobalFlags appGlobalFlags);
        
        ValueTask<IPageWriter> TryGetPageWriterAsync(string topicId, MessagePageId pageId);

    }

    
}