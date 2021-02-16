using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.AzureStorage.QueueSnapshot;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContentCompressed;

namespace MyServiceBus.Persistence.AzureStorage.CompressedMessages
{


    [DataContract]
    public class LastCompressedPageContract
    {
        [DataMember(Order = 1)]
        public string TopicId { get; set; }
        [DataMember(Order = 2)]
        public long PageId { get; set; }
    }
    
    
    public class LastCompressedPageStorage : ILastCompressedPageStorage
    {
        private readonly IAzurePageBlob _azurePageBlob;

        public LastCompressedPageStorage(IAzurePageBlob azurePageBlob)
        {
            _azurePageBlob = azurePageBlob;
        }


        private readonly Dictionary<string, long> _lastPages = new Dictionary<string, long>();

        private bool _initialized = false;

        private bool _hasDataToUpdate;

        private async Task<LastCompressedPageContract[]> ReadContractAsync()
        {
            if (!await _azurePageBlob.ExistsAsync())
                return Array.Empty<LastCompressedPageContract>();
            
            try
            {
                var contract = await _azurePageBlob.ReadAndDeserializeAsProtobufAsync<LastCompressedPageContract[]>();
                return contract ?? Array.Empty<LastCompressedPageContract>();
            }
            catch (Exception)
            {
                Console.WriteLine("Can not read LastCompressedPageStorage content. Initializing");
            }
            
            return Array.Empty<LastCompressedPageContract>();
        }

        private async Task InitializeFromBlobAsync()
        {

            await _azurePageBlob.CreateIfNotExists();
            
            var dataFromBlob = await ReadContractAsync();

            lock (_lastPages)
            {
                foreach (var itm in dataFromBlob)
                {
                    if (_lastPages.ContainsKey(itm.TopicId))
                        _lastPages[itm.TopicId] = itm.PageId;
                    else
                        _lastPages.Add(itm.TopicId, itm.PageId);
                }
            }
            _initialized = true;
        }
        
        private ValueTask InitializeAsync()
        {
            if (_initialized)
                return new ValueTask();
            
      
            var task = InitializeFromBlobAsync();
            return new ValueTask(task);
        }
        
        public ValueTask SaveLastCompressedPageStorageAsync(string topicId, MessagePageId pageId)
        {
            lock (_lastPages)
            {
                if (_lastPages.ContainsKey(topicId))
                    _lastPages[topicId] = pageId.Value;
                else
                    _lastPages.Add(topicId, pageId.Value);
                _hasDataToUpdate = true;
            }

            return new ValueTask();
        }

        public async ValueTask<MessagePageId> GetLastCompressedPageAsync(string topicId)
        {
            await InitializeAsync();

            lock (_lastPages)
            {
                return _lastPages.TryGetValue(topicId, out var result) 
                    ? new MessagePageId(result)  
                    : new MessagePageId(0);
            }
        }


        private LastCompressedPageContract[] GetContractToSave()
        {
            lock (_lastPages)
            {
                return _lastPages.Select(itm => new LastCompressedPageContract
                {
                    TopicId = itm.Key,
                    PageId = itm.Value

                }).ToArray();
            }
            
        }

        private async Task FlushToBlobAsync()
        {
            var contractToSave = GetContractToSave();
            await _azurePageBlob.WriteAsProtobufAsync(contractToSave);
            
            Console.WriteLine("Last Compressed pages state is saved to disk");

            _hasDataToUpdate = false;
        }

        public ValueTask FlushAsync()
        {
            return !_hasDataToUpdate 
                ? new ValueTask() 
                : new ValueTask(FlushToBlobAsync());
        }
    }
}