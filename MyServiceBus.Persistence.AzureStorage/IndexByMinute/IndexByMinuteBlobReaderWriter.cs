using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyAzurePageBlobs.DataBuilder.RandomAccess;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.IndexByMinute;

namespace MyServiceBus.Persistence.AzureStorage.IndexByMinute
{
    
    
    public class IndexByMinuteBlobReaderWriter
    {
        private readonly IAzurePageBlob _azurePageBlob;
        private readonly IAppLogger _appLogger;
        private readonly string _topicId;

        private static int _indexSize;

        private readonly RandomAccessDataBuilder _builder;

        private bool _initialized;

        public IndexByMinuteBlobReaderWriter(IAzurePageBlob azurePageBlob, IAppLogger appLogger, string topicId, int cacheSize = 512)
        {
            _azurePageBlob = azurePageBlob;
            _appLogger = appLogger;
            _topicId = topicId;
            _indexSize = (IndexByMinuteUtils.LastDayOfTheYear+1) * MessagesMinuteUtils.IndexStep;
            _builder = new RandomAccessDataBuilder(azurePageBlob)
                .EnableCaching(cacheSize);
        }
        
        private async ValueTask InitBlobAsync()
        {
            await _azurePageBlob.CreateIfNotExists();
            await _builder.ResizeIfLessAsync(_indexSize);
            _initialized = true;
        }

        private  ValueTask InitAsync()
        {
            return _initialized 
                ? new ValueTask() 
                : InitBlobAsync();
        }

        public async ValueTask WriteAsync(int minute, long messageId)
        {
            if (!_initialized)
                await InitAsync();

            var minuteIndex = MessagesMinuteUtils.GetIndexOffset(minute);
            
             _appLogger?.AddLog(LogProcess .MinuteIndexWriter, _topicId, "MessageId:"+messageId, $"[{_topicId}] Writing minute index with minutes. "+minute + "min=" + messageId + "msgId;");

            await _builder.WriteInt64Async(messageId, minuteIndex);

        }


        public async ValueTask<long> GetMessageIdAsync(int minuteNo)
        {
            if (!_initialized)
                await InitAsync();
            
            var minuteIndex = MessagesMinuteUtils.GetIndexOffset(minuteNo);
            return await _builder.ReadInt64Async(minuteIndex);
        }
    }
  
}