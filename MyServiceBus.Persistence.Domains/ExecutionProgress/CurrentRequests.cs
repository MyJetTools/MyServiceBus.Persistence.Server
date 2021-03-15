using System;
using System.Collections.Generic;
using System.Linq;
using MyServiceBus.Persistence.Domains.MessagesContent;

namespace MyServiceBus.Persistence.Domains.ExecutionProgress
{

    public enum CurrentRequestStatus
    {
        Created, Pending, Executing,
    }

    public class RequestHandler : IDisposable
    {
        private readonly CurrentRequests _currentRequests;
        private static long _nextRequestId;
        public long Id { get; } = _nextRequestId++;
        public string TopicId { get; }
        public string RequestDescription { get; }


        public string CurrentProcess { get; set; } = string.Empty;

        public CurrentRequestStatus Status { get; set; } = CurrentRequestStatus.Created;

        public RequestHandler(CurrentRequests currentRequests, string topicId, string description)
        {
            _currentRequests = currentRequests;
            TopicId = topicId;
            RequestDescription = description;
        }
        
        public void Dispose()
        {
            _currentRequests.DisposeRequest(this);
        }
    }
    
    public class CurrentRequests
    {

        private readonly Dictionary<long, RequestHandler> _requests = new ();

        private IReadOnlyList<RequestHandler> _requestsAsList = Array.Empty<RequestHandler>();


        public RequestHandler StartRequest(TopicDataLocator topicDataLocator, string requestDescription)
        {
            return StartRequest(topicDataLocator.TopicId, requestDescription);
        }

        public RequestHandler StartRequest(string topicId, string requestDescription)
        {
            lock (_requests)
            {
                var newRequest = new RequestHandler(this, topicId, requestDescription);
                _requests.Add(newRequest.Id, newRequest);
                RequestsAmount++;
                _requestsAsList = null;
                return newRequest;
            }
        }


        internal void DisposeRequest(RequestHandler requestHandler)
        {
            lock (_requests)
            {
                _requests.Remove(requestHandler.Id);
                _requestsAsList = null;
                RequestsAmount--;
            }
        }
        
        public int RequestsAmount { get; private set; }

        public IReadOnlyList<RequestHandler> GetAll()
        {
            var currentList = _requestsAsList;
            if (currentList != null)
                return currentList;
            
            lock (_requests)
            {
                _requestsAsList = _requests.Values.ToList();
                return _requestsAsList;
            }
        }
    }
}