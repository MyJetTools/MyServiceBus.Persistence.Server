using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyServiceBus.Persistence.Domains
{
    public static class DictionaryThreadSafe
    {

        public static TValue GetOrCreateThreadSafe<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> createValue)
        {
            lock (dict)
            {
                if (dict.ContainsKey(key))
                    return dict[key];

                var result = createValue();
                dict.Add(key, result);
                return result;
            }
        }
        
        
        private static async Task<TValue> CreateAndGetAsync<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<ValueTask<TValue>> createValueAsync)
        {
            var result = await createValueAsync();

            lock (dict)
            {
                if (!dict.ContainsKey(key))
                    dict.Add(key, result);

                return dict[key];
            }
        }
        
        public static ValueTask<TValue> GetOrCreateThreadSafeAsync<TKey, TValue>(this Dictionary<TKey, TValue> dict,
            TKey key, Func<ValueTask<TValue>> createValueAsync)
        {
            lock (dict)
            {
                return dict.ContainsKey(key) 
                    ? new ValueTask<TValue>(dict[key]) 
                    : new ValueTask<TValue>(CreateAndGetAsync(dict, key, createValueAsync));
            }
        }
        
        
        public static KeyValuePair<TKey, TValue> FirstOrDefaultThreadSafe<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        {
            lock (dict)
            {
                return dict.FirstOrDefault();
            }
        }


        
    }
}