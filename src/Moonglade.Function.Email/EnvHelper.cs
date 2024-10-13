using System.Collections;
using System.Globalization;

namespace Moonglade.Function.Email;

public static class EnvHelper
{
    public static ICollection AllKeys => Environment.GetEnvironmentVariables().Keys;

    public static T Get<T>(string name, T defaultValue = default, EnvironmentVariableTarget? target = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Environment variable name cannot be null or empty.", nameof(name));
        }

        string matchedKey = AllKeys.Cast<string>().FirstOrDefault(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));
        if (matchedKey == null)
        {
            return defaultValue;
        }

        var value = target == null
            ? Environment.GetEnvironmentVariable(matchedKey)
            : Environment.GetEnvironmentVariable(matchedKey, target.Value);

        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }
}