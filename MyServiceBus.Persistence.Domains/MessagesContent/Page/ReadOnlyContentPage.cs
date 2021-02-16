using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent.Page
{
    public class ReadOnlyContentPage : IMessageContentPage
    {

        private SortedDictionary<long, MessageContentGrpcModel> _messages;

        private CompressedPage _compressedPage;

        public DateTime LastAccessTime { get; private set; }
        public int Count { get; private set; }
        public IReadOnlyList<MessageContentGrpcModel> GetMessages()
        {
            return _compressedPage.UnCompress();
        }
        
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

            var result = new SortedDictionary<long, MessageContentGrpcModel>();

            foreach (var message in _messages)
            {
                if (MessagesContentPagesUtils.GetPageId(message.Key).Value == PageId.Value)
                    result.Add(message.Key, message.Value);
            }


            _compressedPage = new CompressedPage(result.Values.ToList()); 
            _messages = result;
            Count = result.Count;
        }
        
        public void InitMessages()
        {
            if (_messages != null)
                return;
            
            var result = new SortedDictionary<long, MessageContentGrpcModel>();
            try
            {
                foreach (var grpcModel in _compressedPage.UnCompress())
                {
                    if (!result.ContainsKey(grpcModel.MessageId))
                        result.Add(grpcModel.MessageId, grpcModel);
                }
                
                Count = result.Count;
            }
            catch (Exception)
            {
                _compressedPage = new CompressedPage(result.Values.ToList());
                Console.WriteLine("Can not Unzip Archive for the page: "+PageId);
            }
            
            _messages = result;

        }


        public void MergeWith(IMessageContentPage messageContentPage)
        {

            if (messageContentPage == null)
                return;
            
            if (_messages == null)
                InitMessages();

            var messages = new SortedDictionary<long, MessageContentGrpcModel>(_messages);

            foreach (var grpcModel in messageContentPage.GetMessages())
            {
                if (!messages.ContainsKey(grpcModel.MessageId))
                    messages.Add(grpcModel.MessageId, grpcModel);
            }

            _messages = messages;
            Count = messages.Count;

        }


        public MessageContentGrpcModel TryGet(long messageId)
        {
            LastAccessTime = DateTime.UtcNow;
            if (_messages == null)
                InitMessages();

            return _messages.ContainsKey(messageId) ? _messages[messageId] : null;
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