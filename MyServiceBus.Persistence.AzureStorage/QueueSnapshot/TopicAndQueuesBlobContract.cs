using System;
using System.Linq;
using System.Runtime.Serialization;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.AzureStorage.QueueSnapshot
{
    [DataContract]
    public class QueueIndexRangeBlobContract
    {
        
        [DataMember(Order = 1)]
        public long FromId { get; set; }
        
        [DataMember(Order = 2)]
        public long ToId { get; set; }

        public static QueueIndexRangeBlobContract Create(QueueIndexRangeGrpcModel src)
        {
            return new QueueIndexRangeBlobContract
            {
                FromId = src.FromId,
                ToId = src.ToId
            };
        }
    }

    [DataContract]
    public class QueueSnapshotBlobContract
    {
        [DataMember(Order = 1)]
        public string QueueId { get; set; }
        
        [DataMember(Order = 2)]
        public QueueIndexRangeBlobContract[] Ranges { get; set; }


        public static QueueSnapshotBlobContract Create(QueueSnapshotGrpcModel src)
        {
            src.Ranges ??= Array.Empty<QueueIndexRangeGrpcModel>();
            
            return new QueueSnapshotBlobContract
            {
                QueueId = src.QueueId,
                Ranges = src.Ranges.Select(QueueIndexRangeBlobContract.Create).ToArray()
            };
        }
    }
    

    [DataContract]
    public class TopicAndQueuesBlobContract 
    {
        
        [DataMember(Order = 1)]
        public string TopicId { get; set; }
        
        [DataMember(Order = 2)]
        public long MessageId { get; set; }

        [DataMember(Order = 3)]
        public int MaxMessagesInCache { get; set; }

        [DataMember(Order = 4)]
        public QueueSnapshotBlobContract[] Snapshots { get; set; }

        public static TopicAndQueuesBlobContract Create(TopicAndQueuesSnapshotGrpcModel topicPersistence)
        {
            topicPersistence.QueueSnapshots ??= Array.Empty<QueueSnapshotGrpcModel>();

            return new TopicAndQueuesBlobContract
            {
                TopicId = topicPersistence.TopicId,
                MessageId = topicPersistence.MessageId,
                Snapshots = topicPersistence.QueueSnapshots.Select(QueueSnapshotBlobContract.Create).ToArray()
            };
        }
        
    }

}