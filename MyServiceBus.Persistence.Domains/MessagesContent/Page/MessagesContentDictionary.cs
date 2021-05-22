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
        
        public bool AddOrUpdate(MessageContentGrpcModel newMessage)
        {

            try
            {

                if (_messages.TryGetValue(newMessage.MessageId, out var existing))
                {
                    if (existing.Created == newMessage.Created)
                        return false;
                    
                    var oldModel = _messages[existing.MessageId];
                    TotalContentSize -= oldModel.Data.Length;
                    _messages[existing.MessageId] = newMessage;
                    TotalContentSize += existing.Data.Length;
                    return true; 
                }
                
                TotalContentSize += newMessage.Data.Length;
                _messages.Add(newMessage.MessageId, newMessage);

                _messagesAsList = null;
                return true;
            }
            finally
            {

                if (MaxMessageId == -1)
                {
                    MaxMessageId = newMessage.MessageId;
                }
                else
                {
                    if (MaxMessageId < newMessage.MessageId)
                        MaxMessageId = newMessage.MessageId;
                }
                
                
                if (MinMessageId == -1)
                {
                    MinMessageId = newMessage.MessageId;
                }
                else
                {
                    if (MinMessageId > newMessage.MessageId)
                        MinMessageId = newMessage.MessageId;
                }
            }
    
        }

        public MessageContentGrpcModel TryGetOrNull(long messageId)
        {
            return _messages.TryGetValue(messageId, out var result) 
                ? result 
                : null;
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

        public long MinMessageId { get; private set; } = -1;
        
        public long MaxMessageId { get; private set; } = -1;


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