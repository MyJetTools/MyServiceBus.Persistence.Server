using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.AzureStorage.CompressedMessages;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Grpc;
using NUnit.Framework;

namespace MyServiceBus.Persistence.Tests
{
    public class TestClusterPageWriter
    {

        private async Task TestPageWriteAndRead(PagesCluster clusterPage, long pageId, Dictionary<long, byte[]> cache)
        {
            var messagePageId = new MessagePageId(pageId);
            var hasPage = await clusterPage.HasPageAsync(messagePageId);
            Assert.IsFalse(hasPage);


            var msg = new MessageContentGrpcModel
            {
                Created = DateTime.UtcNow,
                Data = new [] {(byte)pageId, (byte)pageId, (byte)pageId},
                MessageId = pageId,

            };
            
            var content = new CompressedPage(new []{msg});
            
            cache.Add(pageId, content.ZippedContent.ToArray());
            
            await clusterPage.WriteAsync(messagePageId, content);
            hasPage = await clusterPage.HasPageAsync(messagePageId);
            Assert.IsTrue(hasPage);
            
            var resultContent = await clusterPage.ReadAsync(messagePageId);
            
            resultContent.ZippedContent.AssertAllBytesAreEqualWith(content.ZippedContent);
        }


        private async Task DoubleCheck(PagesCluster clusterPage, long pageId, byte[] srcContent)
        {
            var messagePageId = new MessagePageId(pageId);
            var resultContent = await clusterPage.ReadAsync(messagePageId);
            Assert.AreEqual(resultContent.Messages[0].MessageId, pageId);
            resultContent.ZippedContent.AssertAllBytesAreEqualWith(srcContent);
        }


        [Test]
        public async Task TestThatWeDetectThatPageIsEmpty()
        {

            var azurePageBlob = new MyAzurePageBlobInMem();

            var clusterPage = new PagesCluster(azurePageBlob, new ClusterPageId(0), "test");


            var dict = new Dictionary<long, byte[]>();
            
            
            for (var i = 0; i < 99; i++)
            {
                await TestPageWriteAndRead(clusterPage, i, dict);
            }
            
            
            for (var i = 0; i < 99; i++)
            {
                await DoubleCheck(clusterPage, i, dict[i]);
            }

    
        }
    }
}