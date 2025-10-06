using System;

namespace Grand.SharedKernel.Extensions
{
    /// <summary>
    /// Extension methods for double to help with transitioning from int to double quantities
    /// </summary>
    public static class DoubleExtensions
    {
        /// <summary>
        /// Safely converts a double to int for legacy code
        /// </summary>
        /// <param name="value">The double value to convert</param>
        /// <returns>The value converted to int</returns>
        public static int ToInt(this double value)
        {
            return (int)Math.Round(value);
        }
        
        /// <summary>
        /// Safely converts a nullable double to nullable int for legacy code
        /// </summary>
        /// <param name="value">The nullable double value to convert</param>
        /// <returns>The value converted to nullable int</returns>
        public static int? ToInt(this double? value)
        {
            if (value.HasValue)
                return (int)Math.Round(value.Value);
            return null;
        }
    }
}