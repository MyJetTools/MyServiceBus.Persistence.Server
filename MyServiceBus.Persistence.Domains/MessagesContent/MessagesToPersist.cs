using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.MessagesContent
{
    public class MessagesToPersist
    {

        private readonly Dictionary<long, SortedDictionary<long, MessageContentGrpcModel>>  _messagesToPersist =
            new ();


        public int MessagesToWrite()
        {
            lock (_messagesToPersist)
            {
                return _messagesToPersist.Values.Sum(subDictionary => subDictionary.Count);
            }
        }


        public void Append(MessagePageId pageId, IEnumerable<MessageContentGrpcModel> messages)
        {
            lock (_messagesToPersist)
            {

                var pageDictionary = _messagesToPersist.GetOrCreate(pageId.Value,
                    () => new SortedDictionary<long, MessageContentGrpcModel>());
                
                
                foreach (var message in messages)
                {
                    if (pageDictionary.ContainsKey(message.MessageId))
                        pageDictionary[message.MessageId] = message;
                    else
                        pageDictionary.Add(message.MessageId, message);
                }
            }
        }



    }
}