using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using MyAzureBlob;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.AzureStorage.CompressedMessages;
using MyServiceBus.Persistence.AzureStorage.IndexByMinute;
using MyServiceBus.Persistence.AzureStorage.QueueSnapshot;
using MyServiceBus.Persistence.AzureStorage.TopicMessages;
using MyServiceBus.Persistence.Domains.IndexByMinute;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;
using MyServiceBus.Persistence.Domains.TopicsAndQueues;

namespace MyServiceBus.Persistence.AzureStorage
{
    public static class AzureServicesBinder
    {

        public static void BindTopicsPersistentStorage(this IServiceCollection sc, CloudStorageAccount cloudStorageAccount)
        {
            var blob = new MyAzureBlobContainer(cloudStorageAccount, "system");
            sc.AddSingleton(new LogsSnapshotRepository(blob));
            
            sc.AddSingleton<ITopicsAndQueuesSnapshotStorage>(new TopicsAndQueuesSnapshotStorage(
                new MyAzurePageBlob(cloudStorageAccount, "topics", "topicsdata")));
        }
        
        private const string FileMask = "0000000000000000000";


        public static void BindMessagesPersistentStorage(this IServiceCollection sc, CloudStorageAccount cloudStorageAccount)
        {

            
            sc.AddSingleton<IMessagesContentPersistentStorage>(new MessagesPersistentStorage(parameters =>
            {
                var fileName = parameters.pageId.Value.ToString(FileMask);
                return new MyAzurePageBlob(cloudStorageAccount, parameters.topicId, fileName);
            }));
            
            sc.AddSingleton<ICompressedMessagesStorage>(new CompressedMessagesStorage(parameters =>
            {
                var fileName = "cluster-"+parameters.pageCluserId.Value.ToString(FileMask)+".zip";
                return new MyAzurePageBlob(cloudStorageAccount, parameters.topicId, fileName);
            }));
            
            sc.AddSingleton<ILegacyCompressedMessagesStorage>(new LegacyCompressedMessagesStorage(parameters =>
            {
                var fileName = parameters.pageId.Value.ToString(FileMask)+".zip";
                return new MyAzurePageBlob(cloudStorageAccount, parameters.topicId, fileName);
            }));
            
            sc.AddSingleton<IIndexByMinuteStorage>(new IndexByMinuteStorage(parameters =>
            {
                var fileName = "index-" + parameters.year;
                return new MyAzurePageBlob(cloudStorageAccount, parameters.topicId, fileName);
            }));
            
            sc.AddSingleton<ILastCompressedPageStorage>(new LastCompressedPageStorage(new MyAzurePageBlob(cloudStorageAccount, "system", "last-compressed")));
        }
        
    }
}