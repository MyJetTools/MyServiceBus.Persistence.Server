using System.Collections.Generic;
using System.Linq;

namespace MyServiceBus.Persistence.Domains.BackgroundJobs.PersistentOperations
{
    public static class PersistentOperationsOptimizer
    {

        private static void OptimizeAppendOperation(
            AppendPageDataPersistentOperation appendOperation,
            IList<PersistentOperationBase> operations)
        {

            var otherAppendOperations
                = operations
                    .Select(itm => itm as AppendPageDataPersistentOperation)
                    .Where(itm => itm != null)
                    .Where(itm => itm.PageId.EqualsWith(appendOperation.PageId)  && itm.TopicId == appendOperation.TopicId)
                    .ToList();

            if (otherAppendOperations.Count == 0)
                return;

            foreach (var otherAppendOperation in otherAppendOperations)
            {
                appendOperation.AddMessages(otherAppendOperation);
                operations.Remove(otherAppendOperation);
            }

        }

        public static PersistentOperationBase OptimizeIt(this PersistentOperationBase nextOperation, 
            List<PersistentOperationBase> operations)
        {
            if (nextOperation is AppendPageDataPersistentOperation appendOperation)
                OptimizeAppendOperation(appendOperation, operations);

            return nextOperation;
        }
        
    }
}