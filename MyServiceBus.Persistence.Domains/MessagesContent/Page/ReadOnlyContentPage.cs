using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent.Page
{
    public class ReadOnlyContentPage : IMessageContentPage
    {

        private readonly CompressedPage _compressedPage;

        public DateTime LastAccessTime { get; private set; }  = DateTime.UtcNow;
        public int Count => _compressedPage.Messages.Count;
        public long TotalContentSize => _compressedPage.ZippedContent.Length;

        public IReadOnlyList<MessageContentGrpcModel> GetMessages()
        {
            LastAccessTime = DateTime.UtcNow;
            return _compressedPage.Messages;
        }
        
        public long MinMessageId => _compressedPage.MinMessagesId;
        public long MaxMessageId => _compressedPage.MaxMessagesId;
        public int NotSavedAmount => 0;

        public MessagePageId PageId { get; }

        public ReadOnlyContentPage(CompressedPage compressedPage)
        {
            PageId = compressedPage.PageId;
            _compressedPage = compressedPage;
        }

        public MessageContentGrpcModel TryGet(long messageId)
        {
            LastAccessTime = DateTime.UtcNow;
            return _compressedPage.Messages.FirstOrDefault(itm => itm.MessageId == messageId);
        }

    }

}