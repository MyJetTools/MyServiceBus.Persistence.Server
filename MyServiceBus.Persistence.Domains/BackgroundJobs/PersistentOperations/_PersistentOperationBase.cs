using System;
using System.Threading.Tasks;
using MyDependencies;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations
{
    public abstract class PersistentOperationBase
    {

        public string Id { get; } = Guid.NewGuid().ToString("D").Substring(0,8);

        public string TopicId { get; }
        public MessagePageId PageId { get; }
        
        public string Reason { get; }

        public PersistentOperationBase(string topicId, MessagePageId pageId, string reason)
        {
            TopicId = topicId;
            PageId = pageId;
            Reason = reason;
        }

        protected abstract Task<IMessageContentPage> ExecuteOperationAsync();
        
        public abstract string OperationFriendlyName { get; }
        
        public async Task HandleAsync()
        {
            try
            {
                var result = await ExecuteOperationAsync();
                _completionSource.SetResult(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(GetType());
                Console.WriteLine(TopicId+"/"+PageId);
                Console.WriteLine(e);
                _completionSource.SetException(e);
            }
        }

        private readonly TaskCompletionSource<IMessageContentPage> _completionSource = new TaskCompletionSource<IMessageContentPage>();


        public abstract void Inject(IServiceResolver serviceResolver);
        
        public Task<IMessageContentPage> GetCurrentTask()
        {
            return _completionSource.Task;
        }
    }
    
}