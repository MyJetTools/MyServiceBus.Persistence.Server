using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations
{
    public class AppendPageDataPersistentOperation : PersistentOperationBase
    {

        private readonly SortedDictionary<long, MessageContentGrpcModel> _messagesToPersist
            = new ();


        private readonly long _min;

        public AppendPageDataPersistentOperation(string topicId, MessagePageId pageId, string reason, IEnumerable<MessageContentGrpcModel> messages) 
            : base(topicId, pageId, reason)
        {
            foreach (var message in messages)
                _messagesToPersist.TryAdd(message.MessageId, message);

            _min = _messagesToPersist.Keys.First();

        }

        public void AddMessages(AppendPageDataPersistentOperation operation)
        {
            foreach (var message in operation._messagesToPersist.Values)
                _messagesToPersist.TryAdd(message.MessageId, message);
        }

        public override void Inject(IServiceProvider serviceProvider)
        {
            _messagesContentPersistentStorage = serviceProvider.GetRequiredService<IMessagesContentPersistentStorage>();
            _messagesContentCache = serviceProvider.GetRequiredService<MessagesContentCache>();
            _globalFlags = serviceProvider.GetRequiredService<AppGlobalFlags>();
        }


        private IMessagesContentPersistentStorage _messagesContentPersistentStorage;

        private MessagesContentCache _messagesContentCache;

        private AppGlobalFlags _globalFlags;

        private bool HasFirstMessageOnThePage()
        {
            var messageId = PageId.Value * MessagesContentPagesUtils.MessagesPerPage;
            return _messagesToPersist.ContainsKey(messageId);
        }

        protected override async Task<IMessageContentPage> ExecuteOperationAsync()
        {
            var pageWriter = await _messagesContentPersistentStorage.TryGetPageWriterAsync(TopicId, PageId);

            if (pageWriter == null)
            {
                var page = _messagesContentCache.GetOrCreateWritablePage(TopicId, PageId);
                var init = HasFirstMessageOnThePage();
                pageWriter = await _messagesContentPersistentStorage.CreatePageWriterAsync(TopicId, PageId, init, page, _globalFlags);
            }

            if (!pageWriter.Initialized)
                await pageWriter.WaitUntilInitializedAsync();
            
            await pageWriter.WriteAsync(_messagesToPersist.Values);
            return _messagesContentCache.TryGetPage(TopicId, PageId);
        }

        public override string OperationFriendlyName => "Appending Page Data: "+_min+"...";
    }
}