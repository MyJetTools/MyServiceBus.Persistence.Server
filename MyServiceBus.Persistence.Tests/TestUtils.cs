using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.IndexByMinute;
using NUnit.Framework;

namespace MyServiceBus.Persistence.Tests
{
    public static class TestUtils
    {

        public static void AssertAllBytesAreEqualWith(this ReadOnlyMemory<byte> src, ReadOnlyMemory<byte> dest)
        {
            if (src.Length != dest.Length)
                throw new Exception("Sizes of arrays are different");

            var srcSpan = src.Span;
            var destSpan = dest.Span;

            for (var i = 0; i < src.Length; i++)
            {
                if (srcSpan[i] != destSpan[i])
                    throw new Exception("Arrays are not the same at index: " + i);
            }
        }

        public static IEnumerable<DateTime> GoThroughEveryDay(this int year)
        {
            var dt = new DateTime(year, 01, 01);
            var nextYear = year + 1;

            while (dt.Year<nextYear)
            {
                yield return dt;
                dt = dt.AddMinutes(1);
            }
        }

        public static async ValueTask<ReadOnlyMemory<byte>> ToReadOnlyMemoryAsync(this ValueTask<MemoryStream> memoryStreamAsync)
        {
            var memoryStream = await memoryStreamAsync;
            var buffer = memoryStream.GetBuffer();
            return new ReadOnlyMemory<byte>(buffer, 0, (int)memoryStream.Length);
        }
        
    }
}