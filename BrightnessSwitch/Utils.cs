using System;
using System.Collections.Generic;

namespace BrightnessSwitch
{
    public static class Utils
    {
        public static int GetMaxValIndex<T>(this IList<T> list) where T : IComparable<T>
        {
            if (list.Count == 0)
            {
                return -1;
            }
            var maxIndex = 0;
            var maxVal = list[0];
            for (var i = 1; i < list.Count; i++)
            {
                if (list[i].CompareTo(maxVal) > 0)
                {
                    maxIndex = i;
                    maxVal = list[i];
                }
            }
            return maxIndex;
        }
        
        public static int GetMinValIndex<T>(this IList<T> list) where T : IComparable<T>
        {
            if (list.Count == 0)
            {
                return -1;
            }
            var maxIndex = 0;
            var maxVal = list[0];
            for (var i = 1; i < list.Count; i++)
            {
                if (list[i].CompareTo(maxVal) < 0)
                {
                    maxIndex = i;
                    maxVal = list[i];
                }
            }
            return maxIndex;
        }
    }
}