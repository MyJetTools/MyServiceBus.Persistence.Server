using System;
using System.IO;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Readers.Zip;
using SharpCompress.Writers;

namespace MyServiceBus.Persistence.Domains.MessagesContentCompressed
{
    public static class MessagesCompressor
    {

        public static ReadOnlyMemory<byte> ToReadOnlyMemory(this MemoryStream stream)
        {
            return new (stream.GetBuffer(), 0, (int)stream.Length);
        }

        private const string ZipEntryName = "d";
        

        public static ReadOnlyMemory<byte> Zip(this MemoryStream sourceStream)
        {
            var zipResultStream = new MemoryStream();

            using var zipWriter = WriterFactory.Open(zipResultStream, ArchiveType.Zip, new WriterOptions(CompressionType.Deflate));

            zipWriter.Write(ZipEntryName, sourceStream);

            return zipResultStream.ToArray();
        }

        public static ReadOnlyMemory<byte> Unzip(this ReadOnlyMemory<byte> src)
        {
            
            var srcStream = new MemoryStream(src.Length);
            srcStream.Write(src.Span);
            srcStream.Position = 0;

            var reader = (ZipReader)ReaderFactory.Open(srcStream, new ReaderOptions());

            reader.MoveToNextEntry();
            
            var resultStream = new MemoryStream();
            
            reader.WriteEntryTo(resultStream);

            return resultStream.ToReadOnlyMemory();
        }



    }
}