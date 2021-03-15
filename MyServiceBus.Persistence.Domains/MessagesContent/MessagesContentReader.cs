using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.ExecutionProgress;
using MyServiceBus.Persistence.Domains.IndexByMinute;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent
{
    public class MessagesContentReader
    {
        private readonly TopicsList _topicsList;
        private readonly IMessagesContentPersistentStorage _messagesContentPersistentStorage;
        private readonly ICompressedMessagesStorage _compressedMessagesStorage;
        private readonly IIndexByMinuteStorage _indexByMinuteStorage;
        private readonly AppGlobalFlags _appGlobalFlags;

        public MessagesContentReader(TopicsList topicsList,
            IMessagesContentPersistentStorage messagesContentPersistentStorage,
            ICompressedMessagesStorage compressedMessagesStorage,
            IIndexByMinuteStorage indexByMinuteStorage, AppGlobalFlags appGlobalFlags)
        {
            _topicsList = topicsList;
            _messagesContentPersistentStorage = messagesContentPersistentStorage;
            _compressedMessagesStorage = compressedMessagesStorage;
            _indexByMinuteStorage = indexByMinuteStorage;
            _appGlobalFlags = appGlobalFlags;
        }

        internal async Task<IMessageContentPage> RestorePageUnderLockAsync(TopicDataLocator topicDataLocator, MessagePageId pageId)
        {
            var page = topicDataLocator.TryGetPage(pageId);
            if (page != default)
                return page;
                
            var pageCompressedContent = await _compressedMessagesStorage.GetCompressedPageAsync(topicDataLocator.TopicId, pageId);

            if (pageCompressedContent.Content.Length > 0)
                return pageCompressedContent.ToContentPage(pageId);
                
                    
            var pageWriter = await _messagesContentPersistentStorage.TryGetPageWriterAsync(topicDataLocator.TopicId, pageId);

            if (pageWriter == null)
            {
                var writableContentPage = new WritableContentCachePage(pageId);
                topicDataLocator.AddPage(pageId, writableContentPage);
                pageWriter = await _messagesContentPersistentStorage.CreatePageWriterAsync(topicDataLocator.TopicId, pageId, false, writableContentPage, _appGlobalFlags);
            }
                
            return pageWriter?.GetAssignedPage();
        }

        private async Task<IMessageContentPage> RestorePageAsync(TopicDataLocator topicDataLocator, MessagePageId pageId, RequestHandler requestHandler)
        {
            using (await topicDataLocator.AsyncLock.LockAsync(requestHandler))
            {
                return await RestorePageUnderLockAsync(topicDataLocator, pageId);
            }
        }

        public ValueTask<IMessageContentPage> TryGetPageAsync(string topicId, MessagePageId pageId, RequestHandler requestHandler)
        {
            var topicDataLocator = _topicsList.GetOrCreate(topicId);

            var page = topicDataLocator.TryGetPage(pageId);

            return page != null 
                ? new ValueTask<IMessageContentPage>(page) 
                : new ValueTask<IMessageContentPage>(RestorePageAsync(topicDataLocator, pageId, requestHandler));
        }

        public async Task<(MessageContentGrpcModel message, IMessageContentPage page)> TryGetMessageAsync(string topicId, long messageId, RequestHandler requestHandler)
        {
            var pageId = MessagesContentPagesUtils.GetPageId(messageId);
            
            var page = await TryGetPageAsync(topicId, pageId, requestHandler);
            
            return (page?.TryGet(messageId), page);
        }


        private async ValueTask<long> GetMessageIsByDateAsync(string topicId, DateTime fromDate)
        {
            var minuteWithinTheYear = fromDate.GetMinuteWithinTHeYear();
            var maxMinute = minuteWithinTheYear + 60;

            if (maxMinute > IndexByMinuteUtils.LastDayOfTheYear)
                maxMinute = IndexByMinuteUtils.LastDayOfTheYear;

            while (minuteWithinTheYear < maxMinute)
            {
                var messageId = await _indexByMinuteStorage.GetMessageIdAsync(topicId, fromDate.Year, minuteWithinTheYear);
                if (messageId > 0)
                    return messageId;

                minuteWithinTheYear++;
            }

            return -1;
        }

        public async IAsyncEnumerable<MessageContentGrpcModel> GetMessagesByDate(string topicId, 
            DateTime fromDate, int maxMessagesAmount, RequestHandler requestHandler)
        {
            var messageId = await GetMessageIsByDateAsync(topicId, fromDate);

            if (messageId <= 0) 
                yield break;
            
            await foreach (var item in GetMessagesFromMessageId(topicId, messageId, maxMessagesAmount, requestHandler))
                yield return item;
        }
        
        public async IAsyncEnumerable<MessageContentGrpcModel> GetMessagesByDateAsync(string topicId, DateTime fromDate, RequestHandler requestHandler)
        {
            var messageId = await GetMessageIsByDateAsync(topicId, fromDate);

            if (messageId <= 0) 
                yield break;
            
            var pageId = MessagesContentPagesUtils.GetPageId(messageId);

            var maxPageId = pageId.Value + 2;

            while (pageId.Value <= maxPageId)
            {
                var page = await TryGetPageAsync(topicId, pageId, requestHandler);

                foreach (var message in page.GetMessages())
                {
                    if (message.Created >= fromDate)
                        yield return message;
                }
                
                pageId = pageId.NextPage();
            }

        }


        public async IAsyncEnumerable<MessageContentGrpcModel> GetMessagesFromMessageId(string topicId, long fromMessageId,
            int maxMessagesAmount, RequestHandler requestHandler)
        {
            var pageId = MessagesContentPagesUtils.GetPageId(fromMessageId);


            var maxPageId = pageId.Value + 2;

            var messageNo = 0;

            while (pageId.Value < maxPageId)
            {
                var page = await TryGetPageAsync(topicId, pageId, requestHandler);

                foreach (var message in page.GetMessages())
                {
                    if (message.MessageId >= fromMessageId)
                    {
                        yield return message;
                        messageNo++;
                    }

                    if (messageNo>=maxMessagesAmount)
                        yield break;
                }

                pageId = new MessagePageId(pageId.Value + 1);
            }
        }

    }
}