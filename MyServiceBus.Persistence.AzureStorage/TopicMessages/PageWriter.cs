using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyAzurePageBlobs.DataBuilder.BinaryPackagesSequence;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.AzureStorage.TopicMessages
{
    public class PageWriter : IPageWriter
    {

        private static readonly Dictionary<string, long> WritePosition = new Dictionary<string, long>();

        public static long GetWritePosition(string topicId)
        {
            lock (WritePosition)
            {
                return WritePosition.TryGetValue(topicId, out var result) ? result : 0;
            }
        }
        
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


        private bool _checkThatBlobExists;
        
        public async Task WriteAsync(IEnumerable<MessageContentGrpcModel> messagesToWrite)
        {

            await _taskCompletionSource.Task;
            
            if (!_checkThatBlobExists)
            {
                await _azurePageBlob.CreateIfNotExists();
                _checkThatBlobExists = true;
            }
            
            var itemsToWriter = new List<ReadOnlyMemory<byte>>();
            
            foreach (var grpcModel in messagesToWrite)
            {
                var memoryStream = new MemoryStream();
                ProtoBuf.Serializer.Serialize(memoryStream, grpcModel);
                var memory = memoryStream.GetBuffer();
                itemsToWriter.Add(new ReadOnlyMemory<byte>(memory, 0, (int)memoryStream.Position));
            }

            await _binaryPackagesSequenceBuilder.AppendAsync(itemsToWriter);

            lock (WritePosition)
            {
                if (WritePosition.ContainsKey(TopicId))
                    WritePosition[TopicId] = _binaryPackagesSequenceBuilder.Position;
                else
                    WritePosition.Add(TopicId, _binaryPackagesSequenceBuilder.Position);
            }

        }

        private readonly TaskCompletionSource<WritableContentCachePage> _taskCompletionSource =
            new TaskCompletionSource<WritableContentCachePage>();

        public Task WaitUntilInitializedAsync()
        {
            return _taskCompletionSource.Task;
        }

        public IMessageContentPage GetAssignedPage()
        {
            return _cache;
        }

        private WritableContentCachePage _cache;
        
        public async Task AssignPageAndInitialize(WritableContentCachePage page, AppGlobalFlags appGlobalFlags)
        {
            _cache = page;

            try
            {
                var list = await GetMessagesAsync(appGlobalFlags).ToListAsync();
                _cache.Add(list);
                _taskCompletionSource.SetResult(page);
            }
            catch (Exception e)
            {
                _taskCompletionSource.SetException(e);
            }

        }

        public bool Initialized { get; private set; }


        private async IAsyncEnumerable<MessageContentGrpcModel> GetMessagesAsync(AppGlobalFlags appGlobalFlags)
        {
            _binaryPackagesSequenceBuilder = new BinaryPackagesSequenceBuilder(_azurePageBlob, _pagesReadingAmount, _pagesReadingAmount, 4096);

            if (await _azurePageBlob.ExistsAsync())
            {
                _checkThatBlobExists = true;

                await foreach (var frame in _binaryPackagesSequenceBuilder.InitAndReadAsync(_pagesReadingAmount))
                {
                    if (appGlobalFlags.IsShuttingDown)
                        yield break;
                
                    var contentMessage = ProtoBuf.Serializer.Deserialize<MessageContentGrpcModel>(frame);
                    yield return contentMessage;
                }
            }
            Initialized = true;
        }
        
    }

}