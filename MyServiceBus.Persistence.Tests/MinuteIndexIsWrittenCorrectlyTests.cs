using System;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.AzureStorage.IndexByMinute;
using MyServiceBus.Persistence.Domains.IndexByMinute;
using NUnit.Framework;

namespace MyServiceBus.Persistence.Tests
{
    public class MinuteIndexIsWrittenCorrectlyTests
    {


        [Test]
        public async Task WriteEveryMinute()
        {
            var blob = new MyAzurePageBlobInMem();

            var writer = new IndexByMinuteBlobReaderWriter(blob, "TEST", 9000);

            var now = DateTime.UtcNow;
            foreach (var dt in 2020.GoThroughEveryDay())
            {
                var minuteNo = dt.GetMinuteWithinTHeYear();
                await writer.WriteAsync(minuteNo, minuteNo+1, false);
            }
            
            Console.WriteLine("Saves done "+(DateTime.Now - now));
            now = DateTime.UtcNow;
            foreach (var dt in 2020.GoThroughEveryDay())
            {
                var minuteNo = dt.GetMinuteWithinTHeYear();
                var pageId = await writer.GetMessageIdAsync(minuteNo);
                
               Assert.AreEqual(minuteNo+1, pageId);
            }
            
            Console.WriteLine("Reads done "+(DateTime.Now - now));
            
            

        }
        
    }
}