using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyAzurePageBlobs.DataBuilder.BinaryPackagesSequence;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.AzureStorage.TopicMessages
{
    
    public class PageWriter : IPageWriter, IAsyncDisposable
    {
        
        private static readonly Dictionary<string, long> WritePosition = new ();

        
        //ToDo - не имплементировано
        public static long GetWritePosition(string topicId)
        {
            lock (WritePosition)
            {
                return WritePosition.TryGetValue(topicId, out var result) ? result : 0;
            }
        }

        public static void AppendWritePosition(string topicId, long writePosition)
        {
            lock (WritePosition)
            {

                if (WritePosition.ContainsKey(topicId))
                    WritePosition[topicId] += writePosition;
                else
                    WritePosition.Add(topicId, writePosition);
            }
        }
        
        public static void ResetWritePosition(string topicId)
        {
            lock (WritePosition)
            {

                if (WritePosition.ContainsKey(topicId))
                    WritePosition[topicId] += 0;
                else
                    WritePosition.Add(topicId, 0);
            }
        }
        
        public DateTime LastAccessTime { get; private set; } =DateTime.UtcNow;
        
        private readonly IAzurePageBlob _azurePageBlob;
        private readonly int _pagesReadingAmount;
        private BinaryPackagesSequenceBuilder _binaryPackagesSequenceBuilder;

        public string TopicId { get; }
        
        public MessagePageId PageId { get; }

        public PageWriter(string topicId, MessagePageId pageId, IAzurePageBlob azurePageBlob, int pagesReadingAmount)
        {
            _azurePageBlob = azurePageBlob;
            _pagesReadingAmount = pagesReadingAmount;
            TopicId = topicId;
            PageId = pageId;
        }

        public async ValueTask SyncIfNeededAsync()
        {

            if (_assignedPage.MaxPageId == MaxMessageIdInBlob)
                return;
            
            
            var result = new List<ReadOnlyMemory<byte>>();

            var size = 0;
            
            foreach (var messageContent in _assignedPage.GetMessagesGreaterThen(MaxMessageIdInBlob))
            {
                
                var memoryStream = new MemoryStream();
                ProtoBuf.Serializer.Serialize(memoryStream, messageContent);
                var memory = memoryStream.GetBuffer();
                result.Add(new ReadOnlyMemory<byte>(memory, 0, (int)memoryStream.Position));
                size += messageContent.Data.Length;

                if (size > 1024 * 1024 * 3)
                {
                    await _binaryPackagesSequenceBuilder.AppendAsync(result);
                    result.Clear();
                    size = 0;
                }

            }

            if (result.Count > 0)
                await _binaryPackagesSequenceBuilder.AppendAsync(result);
            
            LastAccessTime = DateTime.UtcNow;

        }

        public WritableContentCachePage GetAssignedPage()
        {
            return _assignedPage;
        }

        private WritableContentCachePage _assignedPage;


        public async ValueTask<bool> BlobExistsAsync()
        {
            return await _azurePageBlob.ExistsAsync();
        }


        public long MaxMessageIdInBlob { get; private set; }


        public async ValueTask CreateAndAssignAsync(WritableContentCachePage page)
        {
            _assignedPage = page;
            await _azurePageBlob.CreateIfNotExists();
        }
        
        public async Task AssignPageAndInitialize(WritableContentCachePage page, AppGlobalFlags appGlobalFlags)
        {
            _assignedPage = page;
            var items = await GetMessagesAsync(appGlobalFlags).ToListAsync();
            _assignedPage.Add(items);

            if (items.Count > 0)
                MaxMessageIdInBlob = items.Max(itm => itm.MessageId);

        }

        private async IAsyncEnumerable<MessageContentGrpcModel> GetMessagesAsync(AppGlobalFlags appGlobalFlags)
        {
            _binaryPackagesSequenceBuilder =
                new BinaryPackagesSequenceBuilder(_azurePageBlob, _pagesReadingAmount, _pagesReadingAmount, 4096);

            await foreach (var frame in _binaryPackagesSequenceBuilder.InitAndReadAsync(_pagesReadingAmount))
            {
                if (appGlobalFlags.IsShuttingDown)
                    yield break;

                var contentMessage = ProtoBuf.Serializer.Deserialize<MessageContentGrpcModel>(frame);
                yield return contentMessage;
            }

        }

        public ValueTask DisposeAsync()
        {
            return SyncIfNeededAsync();
        }

        public bool HasToSync()
        {
            return _assignedPage.MaxPageId > MaxMessageIdInBlob;
        }
    }

}