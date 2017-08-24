using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TestLibrary
{
    public static class CustomExtensionMethods
    {
        public static void Clear<T>(this BlockingCollection<T> blockingCollection)
        {
            if (blockingCollection == null)
                throw new ArgumentNullException(nameof(blockingCollection));

            while (blockingCollection.Count > 0)
            {
                T item;
                blockingCollection.TryTake(out item);
            }
        }

        public static string ToJson(this Dictionary<string, string> dictionaryData)
        {
            var dictionaryDataString = JsonConvert.SerializeObject(dictionaryData);
            return dictionaryDataString
                .Replace("\\\"", "\"")
                .Replace(":\"{", ":{")
                .Replace("}\",", "},");
        }
    }
}