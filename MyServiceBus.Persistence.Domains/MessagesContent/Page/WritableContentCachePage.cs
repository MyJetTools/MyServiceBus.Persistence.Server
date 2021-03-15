using System;
using System.Collections.Generic;
using System.Threading;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent.Page
{
    public class WritableContentCachePage : IMessageContentPage
    {
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ();


        private readonly MessagesContentDictionary _messages = new ();


        private CompressedPage _compressedSnapshot;

        public WritableContentCachePage(MessagePageId pageId)
        {
            PageId = pageId;
            LastAccessTime = DateTime.UtcNow;
        }
        
        public WritableContentCachePage(MessagePageId pageId, IReadOnlyList<MessageContentGrpcModel> initMessages):this(pageId)
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

                _compressedSnapshot.EmptyIt();
            }
            finally
            {
                LastAccessTime = DateTime.UtcNow;
                Count = _messages.Count;
                _readerWriterLockSlim.ExitWriteLock();
            }
        }


        public CompressedPage  GetCompressedPage()
        {
            _readerWriterLockSlim.EnterReadLock();
            try
            {
                if (_compressedSnapshot.Content.Length > 0)
                    return _compressedSnapshot;

                var list = GetMessagesAsList();

                _compressedSnapshot = new CompressedPage(list);

                return _compressedSnapshot;

            }
            finally
            {
                LastAccessTime = DateTime.UtcNow;
                _readerWriterLockSlim.ExitReadLock();
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

        public bool IsCompressed => false;
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

        public static WritableContentCachePage Create(IMessageContentPage messageContent)
        {
            
            var messages = messageContent.GetCompressedPage().UnCompress();

            return new WritableContentCachePage(messageContent.PageId, messages)
            {
                _compressedSnapshot = messageContent.GetCompressedPage(),
            };
        }

    }
}