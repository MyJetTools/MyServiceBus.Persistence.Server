using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.Domains.MessagesContentCompressed
{
    public interface ICompressedMessagesStorage
    {
        Task WriteCompressedPageAsync(string topicId, MessagePageId pageId, CompressedPage pageData);
        
        Task<CompressedPage> GetCompressedPageAsync(string topicId, MessagePageId pageId);

        ValueTask<bool> HasCompressedPageAsync(string topicId, MessagePageId pageId);

    }


    public static class CompressedMessagesStorageExtensions
    {
        public static async ValueTask WriteCompressedPageAsync(this ICompressedMessagesStorage storage,
            string topicId, IMessageContentPage page)
        {
            var compressedPage = page.GetCompressedPage();
            await storage.WriteCompressedPageAsync(topicId, page.PageId, compressedPage);
        }
    }
}