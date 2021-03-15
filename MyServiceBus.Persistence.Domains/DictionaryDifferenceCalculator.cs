using System;
using System.Collections.Generic;

namespace MyServiceBus.Persistence.Domains
{

    public class DictionaryDifferenceResult<TKey, TValue>
    {
        public IReadOnlyDictionary<TKey, TValue> Inserted { get; internal init; }
        public IReadOnlyDictionary<TKey, TValue> Updated { get; internal init; }
        public IReadOnlyDictionary<TKey, TValue> Deleted { get; internal init; }

        public static DictionaryDifferenceResult<TKey, TValue> CreateAsFirstIteration(IReadOnlyDictionary<TKey, TValue> firstSnapshot)
        {
            return new ()
            {
                Inserted = firstSnapshot
            };
        }
    }
    
    
    public static class DictionaryDifferenceCalculator
    {

        public static DictionaryDifferenceResult<TKey, TValue> GetTheDifference<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> nowSnapshot, IReadOnlyDictionary<TKey, TValue> nextSnapshot, 
            Func<TValue, TValue, bool> areEqual)
        {

            Dictionary<TKey, TValue> inserted = null;
            Dictionary<TKey, TValue> updated = null;
            Dictionary<TKey, TValue> deleted = null;

            foreach (var (nowKey, nowValue) in nowSnapshot)
            {

                if (nextSnapshot.TryGetValue(nowKey, out var nextValue))
                {
                    if (areEqual(nowValue, nextValue)) 
                        continue;
                    
                    updated ??= new Dictionary<TKey, TValue>();
                    updated.Add(nowKey, nextValue);
                }
                else
                {
                    deleted ??= new Dictionary<TKey, TValue>();
                    deleted.Add(nowKey, nowValue);
                }
                
            }

            foreach (var (nextKey, nextValue) in nextSnapshot)
            {
                if (nowSnapshot.ContainsKey(nextKey)) 
                    continue;
                
                inserted ??= new Dictionary<TKey, TValue>();
                inserted.Add(nextKey, nextValue);
            }

            if (inserted == null && deleted == null && updated == null)
                return null;

            return new DictionaryDifferenceResult<TKey, TValue>
            {
                Inserted = inserted,
                Updated = updated,
                Deleted = deleted
            };
        }
        
    }
}