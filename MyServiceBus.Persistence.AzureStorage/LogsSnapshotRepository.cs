using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MyAzureBlob;
using MyServiceBus.Persistence.Domains;

namespace MyServiceBus.Persistence.AzureStorage
{
    public class LogsSnapshotRepository
    {
        private readonly IAzureBlobContainer _azureBlobContainer;


        public LogsSnapshotRepository(IAzureBlobContainer azureBlobContainer)
        {
            _azureBlobContainer = azureBlobContainer;
        }
        
        private const string BlobName = "logs-persistence";


        public async ValueTask SaveAsync(IReadOnlyList<LogItem> items)
        {
            var data = Newtonsoft.Json.JsonConvert.SerializeObject(items);
            await _azureBlobContainer.UploadToBlobAsync(BlobName, Encoding.UTF8.GetBytes(data));
        }


        public async ValueTask<IReadOnlyList<LogItem>> LoadAsync()
        {
            try
            {
                var data = await _azureBlobContainer.DownloadBlobAsync(BlobName);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<LogItem>>(Encoding.UTF8.GetString(data.ToArray()) );
            }
            catch (Exception)
            {
                return Array.Empty<LogItem>();
            }

        }
        
    }
}