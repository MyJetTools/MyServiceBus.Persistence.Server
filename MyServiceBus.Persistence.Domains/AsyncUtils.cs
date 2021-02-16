using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyServiceBus.Persistence.Domains
{
    public static class AsyncUtils
    {

        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> src)
        {
            var result = new List<T>();

            await foreach (var itm in src)
            {
                result.Add(itm);
            }

            return result;
        }
        
    }
}