namespace Storehouse.Core
{
    public static class TExtensions
    {
        /// <summary>
        /// Swap two objects
        /// </summary>
        /// <typeparam name="T">Object Type</typeparam>
        /// <param name="current">Current object</param>
        /// <param name="other">Reference object</param>
        /// <returns>Swapped object</returns>
        public static T SwapWith<T>(this T current, ref T other)
        {
            T tmpOther = other;
            other = current;
            return tmpOther;
        }
    }
}
