using System;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;

namespace MyServiceBus.Persistence.AzureStorage.CompressedMessages
{
    
    public struct ClusterPageId
    {
        public ClusterPageId(long value)
        {
            Value = value;
        }
        
        public long Value { get;  }


        public bool EqualsWith(ClusterPageId clusterPageId)
        {
            return clusterPageId.Value == Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
    
    /// <summary>
    /// Index which keeps tracks on Compressed pages.
    /// We have 10 Compressed pages at one file
    /// </summary>
    public class PagesCluster 
    {
        public readonly IAzurePageBlob AzurePageBlob;
        public ClusterPageId ClusterPageId { get;  }
        public string TopicId { get; }
        private byte[] _tocPage;
        
        private const int TocPageBlobPages = 2;
        private const int TocPageSize = MyAzurePageBlobUtils.PageSize*TocPageBlobPages;
        
        public PagesCluster(IAzurePageBlob azurePageBlob, ClusterPageId clusterPageId, string topicId)
        {
            TopicId = topicId;
            AzurePageBlob = azurePageBlob;
            ClusterPageId = clusterPageId;
        }


        private async Task LoadTocAsync()
        {
            var blobSize = await AzurePageBlob.GetBlobSizeAsync();

            if (blobSize == 0)
            {
                _tocPage = new byte[TocPageSize];
                await AzurePageBlob.WriteBytesAsync(_tocPage, 0);
                return;
            }

            var memoryStream = await AzurePageBlob.ReadAsync(0, TocPageBlobPages);
            _tocPage = memoryStream.ToArray(); 
        }

        private async Task InitIndexPageAsync(bool createTocIfNotExists)
        {
            var exists = await AzurePageBlob.ExistsAsync();

            if (exists || createTocIfNotExists)
            {
                if (!exists)
                    await AzurePageBlob.CreateIfNotExists();
                
                await LoadTocAsync();
            }
                
        }

        private const int TocIndexSize = sizeof(uint) + sizeof(int);


        private void CheckRange(int tocIndex)
        {
            if (tocIndex < 0)
                throw new Exception("Compressed TocIndex must be greater or equal 0. Requested: "+tocIndex);
            
            if (tocIndex > 99)
                throw new Exception("Compressed TocIndex must be less then 100. Requested: "+tocIndex);

        }
        
        private async ValueTask<(uint startPage, int dataLength)> GetPagePositionAllocationToc(int tocIndex, 
            bool createTocIfNotExists)
        {

            
            if (_tocPage == null)
               await InitIndexPageAsync(createTocIfNotExists);

            if (_tocPage == null)
                return (0, 0);
            


            try
            {
                
                //If we do not have exception - can remove this try/catch
                CheckRange(tocIndex);
                var offsetIndex = tocIndex * TocIndexSize;

                var position = BitConverter.ToUInt32(_tocPage, offsetIndex);
                var length = BitConverter.ToInt32(_tocPage, offsetIndex  + sizeof(uint));

                return (position, length);
            }
            catch (Exception)
            {
                Console.WriteLine("TocIndex: "+tocIndex);
                throw;
            }
        }



        private async ValueTask WritePagePositionAllocationToc(int tocIndex, long position, long length)
        {
            if (_tocPage == null)
                await InitIndexPageAsync(true);

            CheckRange(tocIndex);

            var offsetIndex = tocIndex * TocIndexSize;

            BitConverter.TryWriteBytes(_tocPage.AsSpan(offsetIndex), position);
            BitConverter.TryWriteBytes(_tocPage.AsSpan(offsetIndex + sizeof(int)), length);

            if (tocIndex < 64)
                await AzurePageBlob.WriteBytesAsync(new ReadOnlyMemory<byte>(_tocPage, 0, MyAzurePageBlobUtils.PageSize), 0);
            else
                await AzurePageBlob.WriteBytesAsync(new ReadOnlyMemory<byte>(_tocPage, MyAzurePageBlobUtils.PageSize, MyAzurePageBlobUtils.PageSize), 1);
        }


        private async Task<uint> GetTheEndOfTheFileAsync()
        {
            var blobSize = await AzurePageBlob.GetBlobSizeAsync();
            var result = blobSize / MyAzurePageBlobUtils.PageSize;
            return (uint) result;
        }

        public async ValueTask<uint> GetNextPageNoToWriteAsync(int tocIndex, int dataSize)
        {
            
            var (position, length) = await GetPagePositionAllocationToc(tocIndex, false);

            if (length == 0)
                return await GetTheEndOfTheFileAsync();

            var nowTakenPages = MyAzurePageBlobUtils.CalculateRequiredPagesAmount(length);

            var newSizePages = MyAzurePageBlobUtils.CalculateRequiredPagesAmount(dataSize);

            if (nowTakenPages >= newSizePages)
                return position;
            
            return await GetTheEndOfTheFileAsync();
        }

        private int GetPageTocIndex(MessagePageId pageId)
        {
            var firstPageNoOnCompressedPage = ClusterPageId.GetFirstPageIdOnCompressedPage();

            return (int)(pageId.Value - firstPageNoOnCompressedPage.Value);
        }


        public async Task WriteAsync(MessagePageId pageId, CompressedPage pageData)
        {

            var tocIndex = GetPageTocIndex(pageId);
            if (_tocPage == null)
                await InitIndexPageAsync(true);

            var nextPageNoToWrite = await GetNextPageNoToWriteAsync(tocIndex, pageData.Content.Length);

            await AzurePageBlob.WriteBytesAsync(pageData.Content, nextPageNoToWrite, new WriteBytesOptions
            {
                SplitRoundTripsPageSize = 4096
            });
            
            await WritePagePositionAllocationToc(tocIndex, nextPageNoToWrite, pageData.Content.Length);
        }

        public async Task<CompressedPage> ReadAsync(MessagePageId pageId)
        {

            try
            {
                //If we do not have exceptions - can remove this try/catch
                GetPageTocIndex(pageId);
     
            }
            catch (Exception e)
            {
                Console.WriteLine("Can not get TocIndex for page: "+pageId.Value+"; TopicId:"+TopicId);
                Console.WriteLine(e);
                throw;
            }
            
            var tocIndex = GetPageTocIndex(pageId);
            var (position, length) = await GetPagePositionAllocationToc(tocIndex, false);
            if (length == 0)
                return CompressedPage.CreateEmpty();

            try
            {
            
                //If we do not have exceptions - can remove this try/catch

                var fullPages = MyAzurePageBlobUtils.CalculateRequiredPagesAmount(length);

                var result = await AzurePageBlob.ReadAsync(position, fullPages);

                var buffer = result.GetBuffer();
            
                return new CompressedPage(new ReadOnlyMemory<byte>(buffer, 0, (int)length));
            }
            catch (Exception)
            {
                Console.WriteLine("Problem with reading page: "+pageId+" TocIndex: "+tocIndex+"; position:"+position+" Length:"+length);
                return CompressedPage.CreateEmpty();
            }

        }


        public async ValueTask<bool> HasPageAsync(MessagePageId pageId)
        {
            var tocIndex = GetPageTocIndex(pageId);
            var (_, length) = await GetPagePositionAllocationToc(tocIndex, false);
            return length > 0;
        }
        
    }
}