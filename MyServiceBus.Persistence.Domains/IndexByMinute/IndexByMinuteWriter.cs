using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.IndexByMinute
{
    public class IndexByMinuteWriter
    {
        private readonly IIndexByMinuteStorage _indexByMinuteStorage;
        private readonly AppGlobalFlags _appGlobalFlags;

        public IndexByMinuteWriter(IIndexByMinuteStorage indexByMinuteStorage, AppGlobalFlags appGlobalFlags)
        {
            _indexByMinuteStorage = indexByMinuteStorage;
            _appGlobalFlags = appGlobalFlags;
        }
        
        private readonly Dictionary<string, List<MessageContentGrpcModel>> _rawMessagesQueue
            = new ();

        public void NewMessages(string topicId, IEnumerable<MessageContentGrpcModel> messages)
        {
            _rawMessagesQueue.Enqueue(topicId, messages);
        }

        private async Task TryToSaveAsync(string topicId, int year,
            IEnumerable<MessageContentGrpcModel> grpcModels)
        {
            var newIndexData = grpcModels.GroupByMinutes();

            if (_appGlobalFlags.DebugTopic == topicId)
                Console.WriteLine($"Trying to save index for {topicId} and year {year}. New Index Data Amount: {newIndexData.Count}");

            if (newIndexData != null)
                foreach (var (minuteNo, messageId) in newIndexData)
                {
                    var messageIdInStorage = await _indexByMinuteStorage.GetMessageIdAsync(topicId, year, minuteNo);

                    if (messageId < messageIdInStorage || messageIdInStorage == 0)
                        await _indexByMinuteStorage.SaveMinuteIndexAsync(topicId, year, minuteNo, messageId);
                }

        }

        public async ValueTask SaveMessagesToStorage()
        {
            var (topicId, messages) = _rawMessagesQueue.Dequeue();

            while (topicId!=null)
            {
                foreach (var group in messages.GroupBy(itm => itm.Created.Year))
                {
                    var year = group.Key;
                    await TryToSaveAsync(topicId, year, group);
                }

                (topicId, messages) = _rawMessagesQueue.Dequeue();
            }

        }
        
    }
}