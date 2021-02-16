using System.Collections.Generic;
using System.Linq;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations
{
    public class ActiveOperations
    {
        
        private readonly Dictionary<string, Dictionary<long, PersistentOperationBase>> _activeOperations
            = new Dictionary<string, Dictionary<long, PersistentOperationBase>>();
        
        public void AddActiveOperation(PersistentOperationBase operation)
        {
            if (!_activeOperations.ContainsKey(operation.TopicId))
                _activeOperations.Add(operation.TopicId, new Dictionary<long, PersistentOperationBase>());
            
            _activeOperations[operation.TopicId].Add(operation.PageId.Value, operation);
        }


        public bool HasActiveOperation(PersistentOperationBase operation)
        {
            if (!_activeOperations.ContainsKey(operation.TopicId))
                return false;

            return _activeOperations[operation.TopicId].ContainsKey(operation.PageId.Value);
        }


        public void RemoveFromActiveOperation(PersistentOperationBase operationBase)
        {
            if (!_activeOperations.ContainsKey(operationBase.TopicId))
                return;

            var topicOperations = _activeOperations[operationBase.TopicId];

            if (topicOperations.ContainsKey(operationBase.PageId.Value))
                topicOperations.Remove(operationBase.PageId.Value);
        }

        public IEnumerable<PersistentOperationBase> GetActiveOperations()
        {
            return _activeOperations.Values.SelectMany(value => value.Values);
        }

        public int Count()
        {
            return _activeOperations.Values.Sum(value => value.Count);
        }
        
    }
}