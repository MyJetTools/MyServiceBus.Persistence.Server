using System;

namespace MyServiceBus.Persistence.Server.Models
{
    public class MessageContentModel
    {
        public DateTime? Created { get; set; }
        public long Id { get; set; }
        public string Content { get; set; }
        
        public int PageSize { get; set; }
    }
}