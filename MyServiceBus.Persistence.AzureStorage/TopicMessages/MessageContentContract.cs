using System;
using System.IO;
using System.Runtime.Serialization;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.AzureStorage.TopicMessages
{
    [DataContract]
    public class MessageContentBlobContract 
    {
        [DataMember(Order = 1)]
        public long MessageId { get; set; }

        [DataMember(Order = 2)]
        public DateTime Created { get; set; }
        
        [DataMember(Order = 3)]
        public byte[] Data { get; set; }

        public static MessageContentBlobContract Create(MessageContentGrpcModel src)
        {
            return new MessageContentBlobContract
            {
                MessageId = src.MessageId,
                Created = src.Created,
                Data = src.Data
            };
        }

    }


    public static class MessageContentContractSerializer
    {
        
        private static readonly byte[] Header = new byte[4];

        
        public static byte[] SerializeContract(this MessageContentGrpcModel src)
        {
            var contract = MessageContentBlobContract.Create(src);
            
            var memStream = new MemoryStream();
            memStream.Write(Header);
            
            ProtoBuf.Serializer.Serialize(memStream, contract);

            var result = memStream.ToArray();

            BitConverter.TryWriteBytes(result.AsSpan(0, Header.Length), result.Length - 4);

            return result;
        }
        
    }
}