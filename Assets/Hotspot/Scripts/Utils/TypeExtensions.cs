using Rhinox.Lightspeed;

namespace Hotspot.Scripts.Utils
{
    public static class TypeExtensions
    {
        public static bool IsBetween(this int number, int min, int max)
        {
            if (min > max)
                Utility.Swap(ref min, ref max);
            return number > min && number < max;
        }

        public static bool IsBetweenIncl(this int number, int min, int max)
        {
            if (min > max)
                Utility.Swap(ref min, ref max);
            return number >= min && number <= max;
        }
    }
}