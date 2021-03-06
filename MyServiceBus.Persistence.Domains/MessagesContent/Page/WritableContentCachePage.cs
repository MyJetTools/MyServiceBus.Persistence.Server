using System;
using System.Collections.Generic;
using System.Threading;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent.Page
{
    public class WritableContentPage : IMessageContentPage
    {
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ();

        private readonly MessagesContentDictionary _messages = new ();

        private List<MessageContentGrpcModel> _messagesToSynchronize = new();

        public long MinMessageId => _messages.MinMessageId;
        public long MaxMessageId => _messages.MaxMessageId;
        public int NotSavedAmount { get; private set; }

        public WritableContentPage(MessagePageId pageId)
        {
            PageId = pageId;
        }

        private void SyncLastAccess()
        {
            LastAccessTime = DateTime.UtcNow;
            Count = _messages.Count;
            NotSavedAmount = _messagesToSynchronize.Count;
        }

        public void NewMessages(IEnumerable<MessageContentGrpcModel> messagesToAdd)
        {
            _readerWriterLockSlim.EnterWriteLock();
            try
            {
                foreach (var grpcModel in messagesToAdd)
                    if (_messages.AddOrUpdate(grpcModel))
                        _messagesToSynchronize.Add(grpcModel);
            }
            finally
            {
                SyncLastAccess();
                _readerWriterLockSlim.ExitWriteLock();
            }
        }



        public IReadOnlyList<MessageContentGrpcModel> GetMessagesToSynchronize()
        {
            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                if (_messagesToSynchronize.Count == 0)
                    return Array.Empty<MessageContentGrpcModel>();

                var result = _messagesToSynchronize;
                _messagesToSynchronize = new List<MessageContentGrpcModel>();
                return result;
            }
            finally
            {
                SyncLastAccess();
                _readerWriterLockSlim.ExitWriteLock();
            }
        }
        
        public void Init(IEnumerable<MessageContentGrpcModel> messagesToAdd)
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
                SyncLastAccess();
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        public DateTime LastAccessTime { get; private set; } =DateTime.UtcNow;
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


    }
}