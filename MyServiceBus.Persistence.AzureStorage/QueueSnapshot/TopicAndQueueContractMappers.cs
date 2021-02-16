using System;
using System.Linq;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.AzureStorage.QueueSnapshot
{
    public static class TopicAndQueueContractMappers
    {

        public static QueueIndexRangeGrpcModel ToGrpcContract(this QueueIndexRangeBlobContract src)
        {
            return new QueueIndexRangeGrpcModel
            {
                FromId = src.FromId,
                ToId = src.ToId
            };
        }

        public static QueueSnapshotGrpcModel ToGrpcContract(this QueueSnapshotBlobContract src)
        {
            return new QueueSnapshotGrpcModel
            {
                QueueId = src.QueueId,
                Ranges = src.Ranges == null 
                    ? Array.Empty<QueueIndexRangeGrpcModel>() 
                    : src.Ranges.Select(itm => itm.ToGrpcContract()).ToArray()
            };
        }

        public static TopicAndQueuesSnapshotGrpcModel ToGrpcContract(this TopicAndQueuesBlobContract src)
        {
            if (src == null)
                return null;
            
            return new TopicAndQueuesSnapshotGrpcModel
            {
                TopicId = src.TopicId,
                MessageId = src.MessageId,
                QueueSnapshots = src.Snapshots == null 
                    ? Array.Empty<QueueSnapshotGrpcModel>() 
                    : src.Snapshots.Select(itm => itm.ToGrpcContract()).ToArray()
            };
        }

        
    }
}