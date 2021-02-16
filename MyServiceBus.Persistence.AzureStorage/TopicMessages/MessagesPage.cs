using System.Collections.Generic;
using System.Threading;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.AzureStorage.TopicMessages
{


    
    public class MessagesPage
    {
        private readonly SortedDictionary<long, MessageContentGrpcModel> _cache = new SortedDictionary<long, MessageContentGrpcModel>();
        
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();
        
        public MessagesPage(MessagePageId pageId)
        {
            PageId = pageId;
        }
        
        public MessagePageId PageId { get; }

        public MessageContentGrpcModel TryGet(long messageId)
        {
            _readerWriterLockSlim.EnterReadLock();
            try
            {
                return _cache.TryGetValue(messageId, out var result) ? result : null;
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }


        public void Add(MessageContentGrpcModel message)
        {
            _readerWriterLockSlim.EnterWriteLock();
            try
            {
                _cache.TryAdd(message.MessageId, message);
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }
    }
}