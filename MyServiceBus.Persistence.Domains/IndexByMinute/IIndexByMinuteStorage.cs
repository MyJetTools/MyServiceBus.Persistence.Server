using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyServiceBus.Persistence.Domains.IndexByMinute
{
    public interface IIndexByMinuteStorage
    {
        ValueTask SaveMinuteIndexAsync(string topicId, int year, int minute, long messageId);

        ValueTask<long> GetMessageIdAsync(string topicId, int year,  int minuteWithinTheYear);

    }
}