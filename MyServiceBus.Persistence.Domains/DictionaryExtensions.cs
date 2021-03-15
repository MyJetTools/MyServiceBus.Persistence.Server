using System;
using System.Collections.Generic;

namespace MyServiceBus.Persistence.Domains
{
    public static class DictionaryExtensions
    {
        
        public static IDictionary<TKey, TValue> AddByCreatingNewDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key,
            Func<TValue> getValue)
        {
            if (dict.ContainsKey(key))
                return dict;

            return new Dictionary<TKey, TValue>(dict) {{key, getValue()}};
        }
        
        public static IDictionary<TKey, TValue> RemoveByCreatingNewDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict.ContainsKey(key))
                return dict;

            var result = new Dictionary<TKey, TValue>(dict);
            result.Remove(key);
            return result;
        }

        public static TValue TryGetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            return dict.TryGetValue(key, out var result) ? result : default;
        }

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key,
            Func<TValue> createValue)
        {
            if (dict.TryGetValue(key, out var result))
                return result;

            result = createValue();
            dict.Add(key, result);

            return result;
        }
        
    }
}