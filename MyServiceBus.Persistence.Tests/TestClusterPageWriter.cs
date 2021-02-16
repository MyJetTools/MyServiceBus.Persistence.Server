using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.AzureStorage.CompressedMessages;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using NUnit.Framework;

namespace MyServiceBus.Persistence.Tests
{
    public class TestClusterPageWriter
    {

        private async Task TestPageWriteAndRead(PagesCluster clusterPage, long pageId)
        {
            var messagePageId = new MessagePageId(pageId);
            var hasPage = await clusterPage.HasPageAsync(messagePageId);
            Assert.IsFalse(hasPage);
            
            var content = new CompressedPage(new [] {(byte)pageId, (byte)pageId, (byte)pageId});
            await clusterPage.WriteAsync(messagePageId, content);
            hasPage = await clusterPage.HasPageAsync(messagePageId);
            Assert.IsTrue(hasPage);
            
            var resultContent = await clusterPage.ReadAsync(messagePageId);
            
            resultContent.Content.AssertAllBytesAreEqualWith(content.Content);
        }


        private async Task DoubleCheck(PagesCluster clusterPage, long pageId)
        {
            var messagePageId = new MessagePageId(pageId);
            var content = new CompressedPage(new [] {(byte)pageId, (byte)pageId, (byte)pageId});
            var resultContent = await clusterPage.ReadAsync(messagePageId);
            resultContent.Content.AssertAllBytesAreEqualWith(content.Content);
        }


        [Test]
        public async Task TestThatWeDetectThatPageIsEmpty()
        {

            var azurePageBlob = new MyAzurePageBlobInMem();

            var clusterPage = new PagesCluster(azurePageBlob, new ClusterPageId(0), "test");


            for (var i = 0; i < 99; i++)
            {
                await TestPageWriteAndRead(clusterPage, i);
            }
            
            
            for (var i = 0; i < 99; i++)
            {
                await DoubleCheck(clusterPage, i);
            }

    
        }
    }
}