using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.Domains.MessagesContent
{

    public interface IPageWriter
    {
        WritableContentCachePage GetAssignedPage();
        long MaxMessageIdInBlob { get; }
        
        MessagePageId PageId { get; }

    }
    
    public interface IMessagesContentPersistentStorage
    {
        Task DeleteNonCompressedPageAsync(string topicId, MessagePageId pageId);
        
        ValueTask<IPageWriter> GetOrCreateAsync(string topicId, MessagePageId pageId);
        
        ValueTask<IPageWriter> TryGetAsync(string topicId, MessagePageId pageId);

        Task SyncAsync(string topicId);

        Task GcAsync(string topicId, MessagePageId pageId);
    }

    
}