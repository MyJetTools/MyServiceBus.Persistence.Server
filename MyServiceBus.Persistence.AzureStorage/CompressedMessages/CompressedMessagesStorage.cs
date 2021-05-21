using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;

namespace MyServiceBus.Persistence.AzureStorage.CompressedMessages
{
    public class CompressedMessagesStorage : ICompressedMessagesStorage
    {
        private readonly Func<(string topicId, ClusterPageId pageCluserId), IAzurePageBlob> _getAzurePageBlob;

        private readonly List<PagesCluster> _cacheOfIndexPages = new List<PagesCluster>();
        public CompressedMessagesStorage(Func<(string topicId, ClusterPageId pageCluserId), IAzurePageBlob> getAzurePageBlob)
        {
            _getAzurePageBlob = getAzurePageBlob;
        }

        private PagesCluster GetPagesCluster(string topicId, MessagePageId messagePageId)
        {

            var compressedPageId = messagePageId.GetClusterPageId();
            
            var result = _cacheOfIndexPages.FindInCache(topicId, compressedPageId);
            if (result != null)
                return result;
            
            var azurePageBlob = _getAzurePageBlob((topicId, compressedPageId));
            result = azurePageBlob.CreatePagesCluster(topicId, compressedPageId);
            
            _cacheOfIndexPages.AddToCache(result);
            return result;
        }

        public async Task WriteCompressedPageAsync(string topicId, MessagePageId pageId, CompressedPage pageData, IAppLogger appLogger)
        {
            appLogger.AddLog(LogProcess.PagesCompressor, topicId,"PageId: "+pageId.Value, $"Compressed page size is: {pageData.Content.Length}");
            var pagesCluster = GetPagesCluster(topicId, pageId);
            await pagesCluster.WriteAsync(pageId, pageData);
        }

        public Task<CompressedPage> GetCompressedPageAsync(string topicId, MessagePageId pageId)
        {
            var pagesCluster = GetPagesCluster(topicId, pageId);
            return pagesCluster.ReadAsync(pageId);
        }

        public ValueTask<bool> HasCompressedPageAsync(string topicId, MessagePageId pageId)
        {
            var pagesCluster = GetPagesCluster(topicId, pageId);
            return pagesCluster.HasPageAsync(pageId);
        }
    }
}