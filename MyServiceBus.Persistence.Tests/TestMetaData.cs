using System;
using System.IO;
using MyServiceBus.Persistence.Grpc;
using NUnit.Framework;

namespace MyServiceBus.Persistence.Tests
{
    public class TestMetaData
    {

        [Test]
        public void TestNullMetaData()
        {
            var packet = new MessageContentGrpcModel
            {
                Created = DateTime.UtcNow,
                Data = new byte[] {1, 2, 3},
                MessageId = 15,
            };


            var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, packet);

            stream.Position = 0;
            
            var result = ProtoBuf.Serializer.Deserialize<MessageContentGrpcModel>(stream);
            
            Assert.AreEqual(packet.Created, result.Created);
            Assert.AreEqual(packet.Data, result.Data);
            Assert.AreEqual(packet.MessageId, result.MessageId);
        }
        
        [Test]
        public void TestNullMetaDataWithEmptyArray()
        {
            var packet = new MessageContentGrpcModel
            {
                Created = DateTime.UtcNow,
                Data = new byte[] {1, 2, 3},
                MessageId = 15,
                MetaData = Array.Empty<MessageContentMetaDataItem>()
            };


            var stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, packet);

            stream.Position = 0;
            
            var result = ProtoBuf.Serializer.Deserialize<MessageContentGrpcModel>(stream);
            
            Assert.AreEqual(packet.Created, result.Created);
            Assert.AreEqual(packet.Data, result.Data);
            Assert.AreEqual(packet.MessageId, result.MessageId);
            Assert.IsNull(result.MetaData);
        }
        
    }
}