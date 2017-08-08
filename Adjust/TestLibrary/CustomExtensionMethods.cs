using System;
using System.Collections.Concurrent;

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
    }
}