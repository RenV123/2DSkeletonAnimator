using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace _2DSkeletonAnimator.Helpers
{
    public static class MyExtensions
    {
        public static int RemoveAll<T>(this ObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                coll.Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }
    }
}