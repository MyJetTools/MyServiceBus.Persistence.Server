using System;
using System.Collections.Generic;

namespace MyServiceBus.Persistence.Server
{
    public static class Utils
    {

        public static IEnumerable<ReadOnlyMemory<byte>> BatchIt(this ReadOnlyMemory<byte> src, int batchSize)
        {
            var remains = src.Length;
            var position = 0;

            while (remains>0)
            {
                var chunkSize = remains > batchSize ? batchSize : remains;
                yield return src.Slice(position, chunkSize);

                position += chunkSize;
                remains -= chunkSize;
            }
        }
        
    }
}