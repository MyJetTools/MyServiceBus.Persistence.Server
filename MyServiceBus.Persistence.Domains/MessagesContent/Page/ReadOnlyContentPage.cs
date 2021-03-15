using System;
using System.Collections.Generic;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent.Page
{
    //ToDo - Оптимизировать как мы читаем с Compressed страницы
    public class ReadOnlyContentPage : IMessageContentPage
    {

        private MessagesContentDictionary _messages = new ();

        private CompressedPage _compressedPage;

        public DateTime LastAccessTime { get; private set; }
        public int Count => _messages.Count;
        public long TotalContentSize => _messages.TotalContentSize;

        public IReadOnlyList<MessageContentGrpcModel> GetMessages()
        {
            return _compressedPage.UnCompress();
        }

        public bool IsCompressed => true;
        
        public MessagePageId PageId { get; }

        public ReadOnlyContentPage(MessagePageId pageId, CompressedPage compressedPage)
        {
            PageId = pageId;
            _compressedPage = compressedPage;
            LastAccessTime = DateTime.UtcNow;
            InitMessages();
        }

        public ReadOnlyContentPage(IMessageContentPage page):this(page.PageId, page.GetCompressedPage())
        {
        
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


            _compressedPage = new CompressedPage(result.GetMessagesAsList()); 
            _messages = result;
        }
        
        public void InitMessages()
        {
            if (_messages != null)
                return;
            
            var messages = new MessagesContentDictionary();

            try
            {
                messages.Init(_compressedPage.UnCompress());
            }
            catch (Exception)
            {
                _compressedPage = new CompressedPage(messages.GetMessagesAsList());
                Console.WriteLine("Can not Unzip Archive for the page: "+PageId);
            }
            
            _messages = messages;
        }


        public void MergeWith(IMessageContentPage messageContentPage)
        {

            if (messageContentPage == null)
                return;
            
            if (_messages == null)
                InitMessages();

            var result = _messages.Clone();

            foreach (var grpcModel in messageContentPage.GetMessages())
            {
                result.AddOrUpdate(grpcModel);
            }

            _compressedPage = new CompressedPage(result.GetMessagesAsList()); 
            _messages = result;
        }


        public MessageContentGrpcModel TryGet(long messageId)
        {
            LastAccessTime = DateTime.UtcNow;
            if (_messages == null)
                InitMessages();

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