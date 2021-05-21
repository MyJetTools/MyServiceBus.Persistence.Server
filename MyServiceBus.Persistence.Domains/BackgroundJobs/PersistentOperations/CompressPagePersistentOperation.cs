using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations
{
    public class CompressPagePersistentOperation : PersistentOperationBase
    {
        private IMessagesContentPersistentStorage _persistentStorage;

        private ICompressedMessagesStorage _compressedMessagesStorage;


        private IAppLogger _appLogger;

        private readonly IMessageContentPage _contentPage;
        
        public CompressPagePersistentOperation(string topicId, MessagePageId pageId, IMessageContentPage contentPage, string reason)
            :base(topicId, pageId, reason)
        {
            _contentPage = contentPage;
        }

        protected override async Task<IMessageContentPage> ExecuteOperationAsync()
        {
            if (await _compressedMessagesStorage.HasCompressedPageAsync(TopicId, PageId))
                return null;

            var compressedPage = _contentPage.GetCompressedPage();


            if (_contentPage.Count == 0)
            {
                Console.WriteLine("No Message to write. Sikipping writing procedure");
                return null;
            }
            
            _appLogger.AddLog(LogProcess.PagesCompressor,   TopicId,  "Page: "+PageId, $"Writing Compressed data for page {PageId}. Messages: "+_contentPage.Count);
            await _compressedMessagesStorage.WriteCompressedPageAsync(TopicId, PageId, compressedPage, _appLogger);
            

            
            _appLogger.AddLog(LogProcess.PagesCompressor, TopicId, "Page: "+PageId, $"Verifying compressed data for page {PageId}");
            var compressedPageToVerify = await _compressedMessagesStorage.GetCompressedPageAsync(TopicId, PageId);

            var messages = compressedPageToVerify.Messages;

            _appLogger.AddLog(LogProcess.PagesCompressor, TopicId, "Page: "+PageId, $"Verified compressed data for page {PageId}. Messages: " + messages.Count);
            
            _appLogger.AddLog(LogProcess.PagesCompressor, TopicId, "Page: "+PageId, $"Deleting Uncompressed data for page {PageId}");
            await _persistentStorage.DeleteNonCompressedPageAsync(TopicId, PageId);

            _appLogger.AddLog(LogProcess.PagesCompressor, TopicId, "Page: "+PageId, "Written Compressed Page: " + PageId +". Messages in the page:"+_contentPage.Count);

            return _contentPage;
        }

        public override void Inject(IServiceProvider serviceProvider)
        {
            _persistentStorage = serviceProvider.GetRequiredService<IMessagesContentPersistentStorage>();
            _appLogger = serviceProvider.GetRequiredService<IAppLogger>();
            _compressedMessagesStorage = serviceProvider.GetRequiredService<ICompressedMessagesStorage>();
        }
        
        public override string OperationFriendlyName => "Compressing Page";
    }
}