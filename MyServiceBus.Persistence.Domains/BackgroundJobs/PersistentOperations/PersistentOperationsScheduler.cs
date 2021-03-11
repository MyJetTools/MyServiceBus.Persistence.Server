using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;
using MyServiceBus.Persistence.Grpc;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations
{

    public class PersistentOperationsScheduler
    {

        public enum EnqueueOption
        {
            Asap,
            Normal
        }


        private readonly object _lockObject = new ();

        private readonly List<PersistentOperationBase> _queue = new ();

        private readonly ActiveOperations _activeOperations = new ();

        private IServiceProvider _serviceResolver;

        public void RegisterServiceResolver(IServiceProvider sr)
        {
            _serviceResolver = sr;
        }

        private void EnqueueOperation(PersistentOperationBase operation, EnqueueOption enqueueOption)
        {
            lock (_lockObject)
            {
                if (enqueueOption == EnqueueOption.Asap)
                {
                    if (_activeOperations.HasActiveOperation(operation))
                        _queue.Insert(0, operation);
                    else
                    {
                        _activeOperations.AddActiveOperation(operation);
                        Task.Run(() => ExecuteOperationAsync(operation));
                    }
                        
                }
                else
                    _queue.Add(operation);
            }

        }
        
        public Task CompressPageAsync(string topicId, IMessageContentPage messageContentPage, string reason)
        {
            var operation = new CompressPagePersistentOperation(topicId, messageContentPage.PageId, messageContentPage, reason);
            EnqueueOperation(operation, EnqueueOption.Normal);
            return operation.GetCurrentTask();
        }


        public void WriteMessagesAsync(string topicId, string reason, MessagePageId pageId,
            IEnumerable<MessageContentGrpcModel> messages)
        {
            var operation = new AppendPageDataPersistentOperation(topicId, pageId, reason, messages);
            EnqueueOperation(operation, EnqueueOption.Normal);
        }

        public Task<IMessageContentPage> RestorePageAsync(string topicId, MessagePageId pageId, string reason)
        {
            var operation = new RestorePagePersistentOperation(topicId, pageId, reason);
            EnqueueOperation(operation, EnqueueOption.Asap);
            return operation.GetCurrentTask();
        }


        private PersistentOperationBase GetNextOperation()
        {

            lock (_lockObject)
            {
                foreach (var operation in _queue.Where(operation => !_activeOperations.HasActiveOperation(operation)))
                {
                    _activeOperations.AddActiveOperation(operation);
                    var result = operation.OptimizeIt(_queue);
                    _queue.Remove(result);
                    return result;
                }

                return null;
            }
        }

        private async Task ExecuteOperationAsync(PersistentOperationBase operation)
        {
            try
            {
                operation.Inject(_serviceResolver);
                await operation.HandleAsync();
            }
            finally
            {
                lock (_lockObject)
                    _activeOperations.RemoveFromActiveOperation(operation);
            }

        }

        public async ValueTask ExecuteOperationAsync()
        {
            var currentOperation = GetNextOperation();

            while (currentOperation != null)
            {
                await ExecuteOperationAsync(currentOperation);
                currentOperation = GetNextOperation();
            }

        }
        
        public int GetQueueSize()
        {
            lock (_lockObject)
            {
                return _queue.Count;
            }
        }

        public IReadOnlyList<PersistentOperationBase> GetAwaiting()
        {
            lock (_lockObject)
            {
                return _queue.ToList();
            }
        }

        public IReadOnlyList<PersistentOperationBase> GetActiveOperations()
        {
            lock (_lockObject)
            {
                return _activeOperations.GetActiveOperations().ToList();
            }
        }

        public bool HasOperationsToExecute()
        {
            lock (_lockObject)
            {
                return _activeOperations.Count()>0 || _queue.Count > 0;
            }

        }
        
    }
}