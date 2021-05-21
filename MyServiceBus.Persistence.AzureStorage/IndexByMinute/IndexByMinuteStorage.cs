using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyAzurePageBlobs;
using MyServiceBus.Persistence.Domains;
using MyServiceBus.Persistence.Domains.IndexByMinute;

namespace MyServiceBus.Persistence.AzureStorage.IndexByMinute
{
    public class IndexByMinuteStorage : IIndexByMinuteStorage
    {
        private readonly IAppLogger _appLogger;
        private readonly Func<(string topicId, int year), IAzurePageBlob> _getBlob;

        private readonly Dictionary<int, Dictionary<string, IndexByMinuteBlobReaderWriter>> _blobs =
            new Dictionary<int, Dictionary<string, IndexByMinuteBlobReaderWriter>>();


        public IndexByMinuteStorage(IAppLogger appLogger)
        {
            _appLogger = appLogger;
        }

        public IndexByMinuteStorage(Func<(string topicId, int year), IAzurePageBlob> getBlob)
        {
            _getBlob = getBlob;
        }


        private IndexByMinuteBlobReaderWriter GetBlob(int year, string topicId)
        {
            lock (_blobs)
            {
                if (!_blobs.ContainsKey(year))
                    _blobs.Add(year, new Dictionary<string, IndexByMinuteBlobReaderWriter>());

                var blobsByYear = _blobs[year];
                if (blobsByYear.ContainsKey(topicId))
                    return blobsByYear[topicId];

                var newBlob = new IndexByMinuteBlobReaderWriter(_getBlob((topicId, year)), _appLogger, topicId);
                blobsByYear.Add(topicId, newBlob);
                return newBlob;

            }
        }
        
        public ValueTask SaveMinuteIndexAsync(string topicId, int year, int minute, long messageId)
        {
            var blob = GetBlob(year, topicId);

            return blob.WriteAsync(minute, messageId);
        }

        public ValueTask<long> GetMessageIdAsync(string topicId, int year, int minuteWithinTheYear)
        {
            var blob = GetBlob(year, topicId);
            return blob.GetMessageIdAsync(minuteWithinTheYear);
        }


    }
}