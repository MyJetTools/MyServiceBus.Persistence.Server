using System;
using System.Collections.Generic;
using System.Threading;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent.Page
{
    public class WritableContentCachePage : IMessageContentPage
    {
        private readonly ReaderWriterLockSlim _readerWriterLockSlim;


        private readonly MessagesContentDictionary _messages = new ();

        public WritableContentCachePage(ReaderWriterLockSlim readerWriterLockSlim, MessagePageId pageId)
        {
            _readerWriterLockSlim = readerWriterLockSlim;
            PageId = pageId;
            LastAccessTime = DateTime.UtcNow;
        }
        
        public WritableContentCachePage(ReaderWriterLockSlim readerWriterLockSlim, MessagePageId pageId, IReadOnlyList<MessageContentGrpcModel> initMessages):
            this(readerWriterLockSlim, pageId)
        {
            _messages.Init(initMessages);
        }

        public void Add(IEnumerable<MessageContentGrpcModel> messagesToAdd)
        {
            _readerWriterLockSlim.EnterWriteLock();
            try
            {
                foreach (var grpcModel in messagesToAdd)
                {
                    _messages.AddOrUpdate(grpcModel);
                }

            }
            finally
            {
                LastAccessTime = DateTime.UtcNow;
                Count = _messages.Count;
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        public DateTime LastAccessTime { get; private set; }
        public int Count { get; private set; }
        public long TotalContentSize => _messages.TotalContentSize;

        public IReadOnlyList<MessageContentGrpcModel> GetMessages()
        {
            return GetMessagesAsList();
        }

        private IReadOnlyList<MessageContentGrpcModel> GetMessagesAsList()
        {
            return _messages.GetMessagesAsList();
        }

        public MessagePageId PageId { get; }

        public MessageContentGrpcModel TryGet(long messageId)
        {
            _readerWriterLockSlim.EnterReadLock();
            try
            {
                return _messages.TryGetOrNull(messageId);
            }
            finally
            {
                LastAccessTime = DateTime.UtcNow;
                _readerWriterLockSlim.ExitReadLock();
            } 
        }


        public  (IReadOnlyList<long> holes, int count)  TestIfThereAreHoles(long pageId)
        {
            _readerWriterLockSlim.EnterReadLock();
            try
            {
                return _messages.TestIfThereAreHoles(pageId);
            }
            finally
            {
                LastAccessTime = DateTime.UtcNow;
                _readerWriterLockSlim.ExitReadLock();
            } 
        }

        public static WritableContentCachePage Create(ReaderWriterLockSlim readerWriterLockSlim, IMessageContentPage messageContent)
        {
            var result =  new WritableContentCachePage(readerWriterLockSlim, messageContent.PageId);
            result._messages.Init(messageContent.GetMessages());
            return result;
        }
        

    }
}