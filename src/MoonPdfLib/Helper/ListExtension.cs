using System;
using System.Collections.Generic;

namespace MoonPdfLib.Helper
{
    internal static class ListExtension
    {
        public static IEnumerable<T> Take<T>(this IList<T> list, int start, int length)
        {
            for (var i = start; i < Math.Min(list.Count, start + length); i++)
            {
                yield return list[i];
            }
        }
    }
}
