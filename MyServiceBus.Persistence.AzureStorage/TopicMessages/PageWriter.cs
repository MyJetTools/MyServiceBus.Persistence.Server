using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyAzurePageBlobs.DataBuilder.BinaryPackagesSequence;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Domains.Metrics;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.AzureStorage.TopicMessages
{
    
    public class PageWriter : IPageWriter, IAsyncDisposable
    {

        private readonly TopicMetrics _topicMetrics;

        private readonly Dictionary<long, MessageContentGrpcModel> _messagesInBlob = new();
        
        private readonly IAzurePageBlob _azurePageBlob;
        private readonly int _pagesReadingAmount;
        private BinaryPackagesSequenceBuilder _binaryPackagesSequenceBuilder;

        public MessagePageId PageId { get; }

        public PageWriter(MessagePageId pageId, IAzurePageBlob azurePageBlob, 
            TopicMetrics topicMetrics, int pagesReadingAmount, WritableContentCachePage writableContentCachePage)
        {
            _azurePageBlob = azurePageBlob;
            _pagesReadingAmount = pagesReadingAmount;
            PageId = pageId;
            _topicMetrics = topicMetrics;
            AssignedPage = writableContentCachePage;
        }


        private async Task UploadToBlobAsync(List<ReadOnlyMemory<byte>> serializedMessages, List<MessageContentGrpcModel> messages)
        {
            await _binaryPackagesSequenceBuilder.AppendAsync(serializedMessages);

            foreach (var message in messages)
            {
                _messagesInBlob.Add(message.MessageId, message);
            }
            
        }


        private async Task SynchronizeMessagesAsync(IReadOnlyList<MessageContentGrpcModel> messagesToUpload)
        {
            var serializedMessages = new List<ReadOnlyMemory<byte>>();
            var grpcMessages = new List<MessageContentGrpcModel>();

            var size = 0;
            
            foreach (var messageContent in messagesToUpload)
            {
                
                var memoryStream = new MemoryStream();
                ProtoBuf.Serializer.Serialize(memoryStream, messageContent);
                var memory = memoryStream.GetBuffer();
                serializedMessages.Add(new ReadOnlyMemory<byte>(memory, 0, (int)memoryStream.Position));
                grpcMessages.Add(messageContent);
                
                
                if (MaxMessageIdInBlob < messageContent.MessageId)
                    MaxMessageIdInBlob = messageContent.MessageId;
                
                size += messageContent.Data.Length;

                if (size > 1024 * 1024 * 3)
                {
                    await UploadToBlobAsync(serializedMessages, grpcMessages);
                    serializedMessages.Clear();
                    grpcMessages.Clear();
                    size = 0;
                }

            }

            if (serializedMessages.Count > 0)
                await UploadToBlobAsync(serializedMessages, grpcMessages);
            
            _topicMetrics.Update(PageId, _binaryPackagesSequenceBuilder.Position, MaxMessageIdInBlob);
        }
        

        public ValueTask SyncIfNeededAsync()
        {
            var newMessages = AssignedPage.GetMessagesToSynchronize();
            
            return newMessages.Count == 0 
                ? new ValueTask() 
                : new ValueTask(SynchronizeMessagesAsync(newMessages));
        }


        public WritableContentCachePage AssignedPage { get; }


        public async ValueTask<bool> BlobExistsAsync()
        {
            return await _azurePageBlob.ExistsAsync();
        }


        public long MaxMessageIdInBlob { get; private set; }


        public async ValueTask CreateAndAssignAsync(AppGlobalFlags appGlobalFlags)
        {
            await _azurePageBlob.CreateIfNotExists();
            await AssignPageAndInitialize(appGlobalFlags);
        }
        
        public async Task AssignPageAndInitialize(AppGlobalFlags appGlobalFlags)
        {
            await LoadMessagesAndInitBlobAsync(appGlobalFlags);
            AssignedPage.Init(_messagesInBlob.Values);
        }

        private async Task LoadMessagesAndInitBlobAsync(AppGlobalFlags appGlobalFlags)
        {
            _binaryPackagesSequenceBuilder =
                new BinaryPackagesSequenceBuilder(_azurePageBlob, _pagesReadingAmount, _pagesReadingAmount, 4096);

            await foreach (var frame in _binaryPackagesSequenceBuilder.InitAndReadAsync(_pagesReadingAmount))
            {
                
                if (appGlobalFlags.IsShuttingDown)
                    break;
                
                var contentMessage = ProtoBuf.Serializer.Deserialize<MessageContentGrpcModel>(frame);
                _messagesInBlob.Add(contentMessage.MessageId, contentMessage);

                if (MaxMessageIdInBlob < contentMessage.MessageId)
                    MaxMessageIdInBlob = contentMessage.MessageId;
                
                _topicMetrics.Update(PageId, _binaryPackagesSequenceBuilder.Position, MaxMessageIdInBlob);
            }
            
            

        }

        public ValueTask DisposeAsync()
        {
            return SyncIfNeededAsync();
        }

        public int MessagesInBlobAmount => _messagesInBlob.Count;
    }

}