using MyServiceBus.Persistence.Domains.TopicsAndQueues;
using MyServiceBus.Persistence.Grpc;
using NUnit.Framework;

namespace MyServiceBus.Persistence.Tests
{
    public class TestActivePagesDetection
    {


        [Test]
        public void TestActivePagesDetectionWithOneMessage()
        {

            var snapshot = new TopicAndQueuesSnapshotGrpcModel
            {
                MessageId = 15,
                QueueSnapshots = new[]
                {
                    new QueueSnapshotGrpcModel
                    {
                        QueueId = "TEST",
                        Ranges = new[]
                        {
                            new QueueIndexRangeGrpcModel
                            {
                                FromId = 10,
                                ToId = 16
                            }

                        }
                    }
                }
            };

            var activePages = snapshot.GetActivePages();
            
            Assert.AreEqual(1, activePages.Count);

        }
    }
}