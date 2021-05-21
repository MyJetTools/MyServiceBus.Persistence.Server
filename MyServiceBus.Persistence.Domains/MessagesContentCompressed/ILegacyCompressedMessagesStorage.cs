using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.Domains.MessagesContentCompressed
{
    public interface ILegacyCompressedMessagesStorage
    {
        Task<CompressedPage> GetCompressedPageAsync(string topicId, MessagePageId pageId);
        public ValueTask<bool> HasCompressedPageAsync(string topicId, MessagePageId pageId);

        ValueTask DeleteIfExistsAsync(string topicId, MessagePageId pageId);
    }



}