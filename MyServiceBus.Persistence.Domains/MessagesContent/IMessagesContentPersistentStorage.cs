using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.Domains.MessagesContent
{

    public interface IPageWriter
    {
        WritableContentPage AssignedPage { get; }
        long MaxMessageIdInBlob { get; }
        
        MessagePageId PageId { get; }
        
        int MessagesInBlobAmount { get; }

    }


    public struct GcWriterResult
    {
        
        public bool NotReadyToGc { get; set; }
        
        public bool NotFound { get; set; }
        
        public IPageWriter DisposedPageWriter { get; set; }
        
    }

    public enum SyncResult
    {
        Done, WriterNotFound
    }
    
    public interface IMessagesContentPersistentStorage
    {
        Task DeleteNonCompressedPageAsync(string topicId, MessagePageId pageId);
        
        ValueTask CreateNewPageAsync(string topicId, MessagePageId pageId, Func<WritableContentPage> getWritableContentCachePage);
        
        ValueTask<IPageWriter> TryGetAsync(string topicId, MessagePageId pageId, Func<WritableContentPage> getWritableContentCachePage);

        Task<SyncResult> SyncAsync(string topicId, MessagePageId pageId);

        ValueTask<GcWriterResult> TryToGcAsync(string topicId, MessagePageId pageId);

        IReadOnlyList<IPageWriter> GetLoadedWriters(string topicId);
    }
    

    
}