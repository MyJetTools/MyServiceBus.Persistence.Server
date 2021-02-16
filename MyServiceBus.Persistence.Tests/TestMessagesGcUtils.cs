using System.Linq;
using MyServiceBus.Persistence.Domains.BackgroundJobs;
using MyServiceBus.Persistence.Domains.MessagesContent;
using NUnit.Framework;

namespace MyServiceBus.Persistence.Tests
{
    public class TestMessagesGcUtils
    {

        [Test]
        public void TestMessagesToWarmUp()
        {
            var pagesInCache = new [] {1, 2, 3, 4}.Select(itm => new MessagePageId(itm)).ToArray();

            var activePages = new long[] {4, 5, 6}.Select(itm => new MessagePageId(itm)).ToArray();


            var pagesToWarmUp = pagesInCache.GetPagesToWarmUp(activePages).ToList();
            
            Assert.AreEqual(2, pagesToWarmUp.Count);
            Assert.AreEqual(5, pagesToWarmUp[0].Value);
            Assert.AreEqual(6, pagesToWarmUp[1].Value);

        }
        
        [Test]
        public void TestMessagesToGc()
        {
            var pagesInCache = new long[] {1, 2, 3, 4}.Select(itm => new MessagePageId(itm)).ToArray();

            var activePages = new long[] {4, 5, 6}.Select(itm => new MessagePageId(itm)).ToArray();
            
            
            var pagesToGc = pagesInCache.GetPagesToGarbageCollect(activePages).ToList();
            
            Assert.AreEqual(3, pagesToGc.Count);
            Assert.AreEqual(1, pagesToGc[0].Value);
            Assert.AreEqual(2, pagesToGc[1].Value);
            Assert.AreEqual(3, pagesToGc[2].Value);

        }
        
    }
}