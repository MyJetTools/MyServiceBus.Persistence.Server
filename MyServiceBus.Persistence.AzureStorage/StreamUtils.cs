using System;
using System.IO;

namespace MyServiceBus.Persistence.AzureStorage
{
    public static class StreamUtils
    {
        public static void WriteInt(this Stream stream, int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            if (BitConverter.TryWriteBytes(buffer, value))
                stream.Write(buffer);
        }
        
        public static int ReadInt(this Stream stream)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            stream.Read(bytes);
            return BitConverter.ToInt32(bytes);
        }
 
    }
}