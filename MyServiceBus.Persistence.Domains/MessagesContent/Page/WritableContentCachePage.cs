using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent.Page
{
    public class WritableContentCachePage : IMessageContentPage
    {
        private readonly ReaderWriterLockSlim _readerWriterLockSlim;

        private readonly SortedDictionary<long, MessageContentGrpcModel> _messages 
            = new SortedDictionary<long, MessageContentGrpcModel>();
        
        private IReadOnlyList<MessageContentGrpcModel> _messagesAsList;

        private CompressedPage _compressedSnapshot;

        public WritableContentCachePage(ReaderWriterLockSlim readerWriterLockSlim, MessagePageId pageId)
        {
            _readerWriterLockSlim = readerWriterLockSlim;
            PageId = pageId;
            LastAccessTime = DateTime.UtcNow;
        }

        public void Add(IEnumerable<MessageContentGrpcModel> messagesToAdd)
        {
            _readerWriterLockSlim.EnterWriteLock();
            try
            {
                foreach (var grpcModel in messagesToAdd)
                {
                    if (_messages.ContainsKey(grpcModel.MessageId))
                        _messages[grpcModel.MessageId] = grpcModel;
                    else
                        _messages.Add(grpcModel.MessageId, grpcModel);
                }

                _messagesAsList = null;
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
        public IReadOnlyList<MessageContentGrpcModel> GetMessages()
        {
            return GetMessagesAsList();
        }

        private IReadOnlyList<MessageContentGrpcModel> GetMessagesAsList()
        {
            return _messagesAsList ??= _messages.Values.ToList();
        }

        public MessagePageId PageId { get; }

        public MessageContentGrpcModel TryGet(long messageId)
        {
            _readerWriterLockSlim.EnterReadLock();
            try
            {
                return _messages.TryGetValue(messageId, out var result) ? result : null;
            }
            finally
            {
                LastAccessTime = DateTime.UtcNow;
                _readerWriterLockSlim.ExitReadLock();
            } 
        }

        public static WritableContentCachePage Create(ReaderWriterLockSlim readerWriterLockSlim, IMessageContentPage messageContent)
        {

            var result = new WritableContentCachePage(readerWriterLockSlim, messageContent.PageId)
            {
                _compressedSnapshot = messageContent.GetCompressedPage(),
                _messagesAsList = messageContent.GetCompressedPage().UnCompress()
            };

            foreach (var message in result._messagesAsList)
            {
                if (!result._messages.ContainsKey(message.MessageId))
                    result._messages.Add(message.MessageId, message);
            }

            return result;

        }

    }
}