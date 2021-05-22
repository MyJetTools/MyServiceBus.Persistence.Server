using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent.Page
{
    public class ReadOnlyContentPage : IMessageContentPage
    {

        private readonly MessagesContentDictionary _messages = new ();

        private readonly CompressedPage _compressedPage;

        public DateTime LastAccessTime { get; private set; }
        public int Count => _messages.Count;
        public long TotalContentSize => _messages.TotalContentSize;

        public IReadOnlyList<MessageContentGrpcModel> GetMessages()
        {
            return _compressedPage.Messages;
        }

        public bool HasSkippedId => _messages.HasSkippedId;
        
        public MessagePageId PageId { get; }

        public ReadOnlyContentPage(CompressedPage compressedPage)
        {
            PageId = compressedPage.PageId;
            _compressedPage = compressedPage;
            LastAccessTime = DateTime.UtcNow;
        }

        public MessageContentGrpcModel TryGet(long messageId)
        {
            LastAccessTime = DateTime.UtcNow;
   

            return _messages.TryGetOrNull(messageId);
        }

    }

}