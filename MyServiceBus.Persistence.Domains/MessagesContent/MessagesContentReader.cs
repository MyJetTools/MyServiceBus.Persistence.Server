using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.IndexByMinute;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Domains.PersistenceOperations;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent
{
    public class MessagesContentReader
    {
        private readonly MessagesContentCache _messagesContentCache;

        private readonly IIndexByMinuteStorage _indexByMinuteStorage;
        private readonly TaskSchedulerByTopic _taskSchedulerByTopic;
        private readonly RestorePageFromBlobOperation _restorePageFromBlobOperation;

        public MessagesContentReader(MessagesContentCache messagesContentCache,
            IIndexByMinuteStorage indexByMinuteStorage, TaskSchedulerByTopic taskSchedulerByTopic, 
            RestorePageFromBlobOperation restorePageFromBlobOperation)
        {
            _messagesContentCache = messagesContentCache;
            _indexByMinuteStorage = indexByMinuteStorage;
            _taskSchedulerByTopic = taskSchedulerByTopic;
            _restorePageFromBlobOperation = restorePageFromBlobOperation;
        }



        public async Task<IMessageContentPage> LoadPageIntoCacheTopicSynchronizedAsync(string topicId, MessagePageId pageId)
        {
            var page = _messagesContentCache.TryGetPage(topicId, pageId);

            if (page != null)
                return page;
            
            return await _restorePageFromBlobOperation.TryRestoreFromCompressedPage(topicId, pageId) 
                     ?? await _restorePageFromBlobOperation.TryRestoreFromUncompressedPage(topicId, pageId);
            
        }



        public async ValueTask<IMessageContentPage> TryGetPageTopicThreadSynchronizedAsync(string topicId,
            MessagePageId pageId)
        {
            var page = _messagesContentCache.TryGetPage(topicId, pageId);

            if (page != null)
                return page;

            return await LoadPageIntoCacheTopicSynchronizedAsync(topicId, pageId);

        }
        

        public async ValueTask<IMessageContentPage> TryGetPageAsync(string topicId, MessagePageId pageId, string reason)
        {
            var page = _messagesContentCache.TryGetPage(topicId, pageId);

            if (page != null)
                return page;


            IMessageContentPage result = null;


            await _taskSchedulerByTopic.ExecuteTaskAsync(topicId, pageId, "Getting page: " + reason, async () =>
            {
                result = await TryGetPageTopicThreadSynchronizedAsync(topicId, pageId);
            });

            return result;
        }

        public async Task<(MessageContentGrpcModel message, IMessageContentPage page)> TryGetMessageAsync(string topicId, long messageId, string reason)
        {
            var pageId = MessagesContentPagesUtils.GetPageId(messageId);
            
            var page = await TryGetPageAsync(topicId, pageId, reason);
            
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

        public async IAsyncEnumerable<MessageContentGrpcModel> GetMessagesByDate(string topicId, DateTime fromDate, int maxMessagesAmount
            , string reason)
        {
            var messageId = await GetMessageIsByDateAsync(topicId, fromDate);

            if (messageId <= 0) 
                yield break;
            
            await foreach (var item in GetMessagesFromMessageId(topicId, messageId, maxMessagesAmount, reason))
                yield return item;
        }
        
        public async IAsyncEnumerable<MessageContentGrpcModel> GetMessagesByDateAsync(string topicId, DateTime fromDate, string reason)
        {
            var messageId = await GetMessageIsByDateAsync(topicId, fromDate);

            if (messageId <= 0) 
                yield break;
            
            var pageId = MessagesContentPagesUtils.GetPageId(messageId);

            var maxPageId = pageId.Value + 2;

            while (pageId.Value <= maxPageId)
            {
                var page = await TryGetPageAsync(topicId, pageId, reason);

                foreach (var message in page.GetMessages())
                {
                    if (message.Created >= fromDate)
                        yield return message;
                }
                
                pageId = pageId.NextPage();
            }

        }


        public async IAsyncEnumerable<MessageContentGrpcModel> GetMessagesFromMessageId(string topicId, long fromMessageId,
            int maxMessagesAmount, string reason)
        {
            var pageId = MessagesContentPagesUtils.GetPageId(fromMessageId);


            var maxPageId = pageId.Value + 2;

            var messageNo = 0;

            while (pageId.Value < maxPageId)
            {
                var page = await TryGetPageAsync(topicId, pageId, reason);

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