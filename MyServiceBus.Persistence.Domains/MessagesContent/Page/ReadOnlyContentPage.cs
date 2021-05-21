using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent.Page
{
    public class ReadOnlyContentPage : IMessageContentPage
    {

        private MessagesContentDictionary _messages = new ();

        private CompressedPage _compressedPage;

        public DateTime LastAccessTime { get; private set; }
        public int Count => _messages.Count;
        public long TotalContentSize => _messages.TotalContentSize;

        public IReadOnlyList<MessageContentGrpcModel> GetMessages()
        {
            return _compressedPage.Messages;
        }

        public  (IReadOnlyList<long> holes, int count)  TestIfThereAreHoles(long pageId)
        {
            throw new Exception("Not supported for Read only page");
        }

        public MessagePageId PageId { get; }


        public ReadOnlyContentPage(CompressedPage compressedPage)
        {
            PageId = compressedPage.PageId;
            _compressedPage = compressedPage;
            LastAccessTime = DateTime.UtcNow;
        }


        public ReadOnlyContentPage(IMessageContentPage page)
        {
            PageId = page.PageId;
            LastAccessTime = DateTime.UtcNow;
            _compressedPage = new CompressedPage(page.PageId, page.GetMessages());
        }

        public void FilterOnlyMessagesBelongsToThePage()
        {
            if (_messages == null)
                return;

            var result = new MessagesContentDictionary();

            foreach (var message in _messages.GetMessages())
            {
                if (MessagesContentPagesUtils.GetPageId(message.MessageId).Value == PageId.Value)
                {
                    result.AddOrUpdate(message);
                }
            }


            _compressedPage = new CompressedPage(PageId, result.GetMessagesAsList()); 
            _messages = result;
        }


        public void MergeWith(IMessageContentPage messageContentPage)
        {

            var mergedMessages = new Dictionary<long, MessageContentGrpcModel>();


            foreach (var message in _compressedPage.Messages)
            {
                if (!mergedMessages.ContainsKey(message.MessageId))
                    mergedMessages.Add(message.MessageId, message);
            }

            foreach (var message in messageContentPage.GetMessages())
            {
                if (mergedMessages.ContainsKey(message.MessageId))
                    mergedMessages[message.MessageId] = message;
                else
                    mergedMessages.Add(message.MessageId, message);
            }

  

            _compressedPage = new CompressedPage(PageId, mergedMessages.Values.ToList()); 
        }


        public MessageContentGrpcModel TryGet(long messageId)
        {
            LastAccessTime = DateTime.UtcNow;
   

            return _messages.TryGetOrNull(messageId);
        }

        public CompressedPage GetCompressedPage()
        {
            LastAccessTime = DateTime.UtcNow;
            return _compressedPage;
        }

    }

    public static class ReadOnlyContentPageExtensions
    {
        public static ReadOnlyContentPage ToReadOnlyContentPage(this IMessageContentPage messageContentPage)
        {

            if (messageContentPage == null)
                return null;
            
            if (messageContentPage is ReadOnlyContentPage result)
                return result;

            return new ReadOnlyContentPage(messageContentPage);
        }
    }
}