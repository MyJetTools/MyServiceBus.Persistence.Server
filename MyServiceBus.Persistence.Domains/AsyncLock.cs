using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyServiceBus.Persistence.Domains.ExecutionProgress;

namespace MyServiceBus.Persistence.Domains
{
    public readonly struct LockHandler : IDisposable
    {
        private readonly AsyncLock _lockObject;

        public LockHandler(AsyncLock lockObject)
        {
            _lockObject = lockObject;
        }
        
        public void Dispose()
        {
            _lockObject.Unlock();
        }
    }
    
    
    public class AsyncLock
    {
        private int _lockAmount;

        private readonly object _lockObject = new();

        private readonly Queue<(TaskCompletionSource<LockHandler> taskCompletionSource, RequestHandler request)> _awaitingLocks = new ();
        
        public ValueTask<LockHandler> LockAsync(RequestHandler requestHandler)
        {
            lock (_lockObject)
            {
                if (_lockAmount == 0)
                {
                    _lockAmount++;
                    requestHandler.Status = CurrentRequestStatus.Executing;
                    return new ValueTask<LockHandler>(new LockHandler(this));
                }

                requestHandler.Status = CurrentRequestStatus.Pending;
                var awaitingLock = new TaskCompletionSource<LockHandler>();
                _awaitingLocks.Enqueue((awaitingLock, requestHandler));
                return new ValueTask<LockHandler>(awaitingLock.Task);
            }
        }

        internal void Unlock()
        {
            (TaskCompletionSource<LockHandler> awaitingLock, RequestHandler request) result = default; 
            lock (_lockObject)
            {
                _lockAmount--;
                if (_awaitingLocks.Count > 0)
                    result = _awaitingLocks.Dequeue();
            }

            if (result.awaitingLock == null)
                return;
            
            result.request.Status = CurrentRequestStatus.Executing;
            result.awaitingLock.SetResult(new LockHandler(this));

        }
    }
}