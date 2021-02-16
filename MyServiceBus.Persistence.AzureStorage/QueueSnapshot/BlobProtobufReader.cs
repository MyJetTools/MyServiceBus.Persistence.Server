using System;
using System.IO;
using System.Threading.Tasks;
using MyAzurePageBlobs;

namespace MyServiceBus.Persistence.AzureStorage.QueueSnapshot
{
    public static class BlobProtobufReader
    {
        
        private static readonly byte[] Header = {0, 0, 0, 0};

        public static async Task<T> ReadAndDeserializeAsProtobufAsync<T>(this IAzurePageBlob pageBlob)
        {
            try
            {
                var data = await pageBlob.DownloadAsync();
                data.Position = 0;
                var size = data.ReadInt();
                data.SetLength(size + 4);
                var result = ProtoBuf.Serializer.Deserialize<T>(data);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return default;
            }
        }
        
        public static async Task WriteAsProtobufAsync(this IAzurePageBlob pageBlob, object instance)
        {
            var stream = new MemoryStream();
            stream.Write(Header);
            ProtoBuf.Serializer.Serialize(stream, instance);
            var length = (int) stream.Length - 4;
            
            stream.MakeMemoryStreamReadyForPageBlobWriteOperation();
            stream.Position = 0;
            stream.WriteInt(length);
            stream.Position = 0;
            await pageBlob.WriteAsync(stream, 0);
        }
        
    }
}