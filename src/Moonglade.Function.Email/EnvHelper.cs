using System.Collections;
using System.Globalization;

namespace Moonglade.Function.Email;

public static class EnvHelper
{
    public static ICollection AllKeys => Environment.GetEnvironmentVariables().Keys;

    public static T Get<T>(string name, T defaultValue = default, EnvironmentVariableTarget? target = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Environment variable name cannot be null, empty, or whitespace.", nameof(name));
        }

        // Directly get the environment variable value instead of searching through all keys
        var value = target == null
            ? Environment.GetEnvironmentVariable(name)
            : Environment.GetEnvironmentVariable(name, target.Value);

        // Return default if variable doesn't exist or is empty
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        // Handle special cases for common types
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }

        if (typeof(T) == typeof(bool))
        {
            if (bool.TryParse(value, out var boolResult))
            {
                return (T)(object)boolResult;
            }
            // Handle common boolean representations
            var normalizedValue = value.ToLowerInvariant();
            var isTruthy = normalizedValue is "yes" or "on" or "enabled";
            return (T)(object)isTruthy;
        }

        // Try to convert to the target type
        try
        {
            // Handle nullable types
            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            
            var convertedValue = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
            return (T)convertedValue;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            // Log the conversion error if needed
            // Consider using ILogger here in a real application
            return defaultValue;
        }
    }

    // Additional helper methods for common scenarios
    public static string GetString(string name, string defaultValue = "") =>
        Get(name, defaultValue);

    public static int GetInt(string name, int defaultValue = 0) =>
        Get(name, defaultValue);

    public static bool GetBool(string name, bool defaultValue = false) =>
        Get(name, defaultValue);

    public static double GetDouble(string name, double defaultValue = 0.0) =>
        Get(name, defaultValue);
}