namespace DakLakCoffeeSupplyChain.Common.Helpers
{
    public static class EnumHelper
    {
        /// <summary>
        /// Safely parses a string to the given enum type. If parsing fails, returns the provided default value.
        /// </summary>
        /// <typeparam name="TEnum">Enum type</typeparam>
        /// <param name="value">String to parse</param>
        /// <param name="defaultValue">Default value to use if parsing fails</param>
        /// <returns>Parsed enum value or defaultValue</returns>
        public static TEnum ParseEnumFromString<TEnum>(string? value, TEnum defaultValue) where TEnum : struct, System.Enum
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            return System.Enum.TryParse<TEnum>(value, true, out var result)
                ? result
                : defaultValue;
        }
    }
}
