using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent.Page
{
    public class MessagesContentDictionary
    {
        private readonly SortedDictionary<long, MessageContentGrpcModel> _messages 
            = new ();
        
        private IReadOnlyList<MessageContentGrpcModel> _messagesAsList;
        
        public void AddOrUpdate(MessageContentGrpcModel model)
        {
            
            if (_messages.ContainsKey(model.MessageId))
            {
                var oldModel = _messages[model.MessageId];
                TotalContentSize -= oldModel.Data.Length;
                _messages[model.MessageId] = model;
                TotalContentSize += model.Data.Length;
                return;
            }
            
            TotalContentSize += model.Data.Length;
            _messages.Add(model.MessageId, model);

            _messagesAsList = null;
        }

        public MessageContentGrpcModel TryGetOrNull(long messageId)
        {
            return _messages.TryGetValue(messageId, out var result) 
                ? result 
                : null;
        }

        public IEnumerable<MessageContentGrpcModel> GetMessages()
        {
            return _messages.Values;
        }
        
        public long TotalContentSize { get; private set; }


        public int Count => _messages.Count;
        
        public IReadOnlyList<MessageContentGrpcModel> GetMessagesAsList()
        {
            return _messagesAsList ??= _messages.Values.ToList();
        }

        public void Init(IEnumerable<MessageContentGrpcModel> messagesToInit)
        {
            _messages.Clear();
            TotalContentSize = 0;
            foreach (var grpcModel in messagesToInit)
                AddOrUpdate(grpcModel);
        }



        public  (IReadOnlyList<long> holes, int count)  TestIfThereAreHoles(long pageId)
        {
            List<long> result = null;
            var minPage = pageId * MessagesContentPagesUtils.MessagesPerPage;

            var maxPage = minPage + _messages.Count;

            for (var i = minPage; i < maxPage; i++)
            {
                if (!_messages.ContainsKey(i))
                {
                    result ??= new List<long>();
                    result.Add(i);
                }
            }

            return (result, _messages.Count);
        }

    }
}