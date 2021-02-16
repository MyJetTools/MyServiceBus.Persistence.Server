using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent;

namespace MyServiceBus.Persistence.Domains.MessagesContentCompressed
{
    public interface ILastCompressedPageStorage
    {
        ValueTask SaveLastCompressedPageStorageAsync(string topicId, MessagePageId pageId);

        ValueTask<MessagePageId> GetLastCompressedPageAsync(string topicId);

        ValueTask FlushAsync();
    }
    
}