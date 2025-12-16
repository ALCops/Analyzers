using System.Reflection;

namespace ALCops.Common.Reflection;

/// <summary>
/// Provides safe property access methods using reflection.
/// These methods are designed to maintain compatibility across different API versions
/// by safely accessing properties that may not exist in all versions.
/// </summary>
public static class PropertyAccessor
{
    /// <summary>
    /// Safely sets a property value on an object using reflection.
    /// If the property doesn't exist or can't be set, the operation is silently ignored.
    /// This maintains compatibility across different API versions.
    /// </summary>
    /// <param name="target">The object on which to set the property.</param>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to set on the property.</param>
    /// <returns>True if the property was successfully set, false otherwise.</returns>
    public static bool SetPropertyIfExists(this object target, string propertyName, object? value)
    {
        try
        {
            var propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(target, value);
                return true;
            }

            // Also check base types for the property
            var baseType = target.GetType().BaseType;
            while (baseType != null)
            {
                propertyInfo = baseType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(target, value);
                    return true;
                }
                baseType = baseType.BaseType;
            }
        }
        catch (Exception)
        {
            // Silently ignore if property doesn't exist or can't be set
            // This maintains compatibility across different API versions
        }

        return false;
    }

    /// <summary>
    /// Safely gets a property value from an object using reflection.
    /// If the property doesn't exist or can't be read, returns the default value.
    /// This maintains compatibility across different API versions.
    /// </summary>
    /// <typeparam name="T">The expected type of the property value.</typeparam>
    /// <param name="target">The object from which to get the property.</param>
    /// <param name="propertyName">The name of the property to get.</param>
    /// <param name="defaultValue">The default value to return if the property doesn't exist or can't be read.</param>
    /// <returns>The property value if found and readable, otherwise the default value.</returns>
    public static T? GetPropertyIfExists<T>(this object target, string propertyName, T? defaultValue = default)
    {
        try
        {
            var propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo != null && propertyInfo.CanRead)
            {
                var value = propertyInfo.GetValue(target);
                if (value is T typedValue)
                    return typedValue;
            }

            // Also check base types for the property
            var baseType = target.GetType().BaseType;
            while (baseType != null)
            {
                propertyInfo = baseType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.CanRead)
                {
                    var value = propertyInfo.GetValue(target);
                    if (value is T typedValue)
                        return typedValue;
                }
                baseType = baseType.BaseType;
            }
        }
        catch (Exception)
        {
            // Silently ignore if property doesn't exist or can't be read
            // This maintains compatibility across different API versions
        }

        return defaultValue;
    }
}
