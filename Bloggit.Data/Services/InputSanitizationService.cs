using Bloggit.Data.IServices;
using Ganss.Xss;
using System.Reflection;

namespace Bloggit.Data.Services;

/// <summary>
/// Service for sanitizing user input to prevent XSS and other injection attacks
/// Uses HtmlSanitizer library for robust HTML sanitization
/// </summary>
public class InputSanitizationService : IInputSanitizationService
{
    private readonly HtmlSanitizer _sanitizer;

    public InputSanitizationService()
    {
        _sanitizer = new HtmlSanitizer();

        // Configure allowed tags and attributes for rich text content
        // Remove all potentially dangerous tags
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("br");
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("em");
        _sanitizer.AllowedTags.Add("u");
        _sanitizer.AllowedTags.Add("h1");
        _sanitizer.AllowedTags.Add("h2");
        _sanitizer.AllowedTags.Add("h3");
        _sanitizer.AllowedTags.Add("h4");
        _sanitizer.AllowedTags.Add("h5");
        _sanitizer.AllowedTags.Add("h6");
        _sanitizer.AllowedTags.Add("ul");
        _sanitizer.AllowedTags.Add("ol");
        _sanitizer.AllowedTags.Add("li");
        _sanitizer.AllowedTags.Add("a");
        _sanitizer.AllowedTags.Add("blockquote");
        _sanitizer.AllowedTags.Add("code");
        _sanitizer.AllowedTags.Add("pre");

        // Configure allowed attributes
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("title");

        // Allow data URIs for images but sanitize them
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");

        // Remove all CSS
        _sanitizer.AllowedCssProperties.Clear();
    }

    public string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // Sanitize HTML content
        return _sanitizer.Sanitize(input);
    }

    /// <summary>
    /// Recursively sanitizes all string properties of an object and its nested objects.
    /// Handles circular references to prevent infinite loops.
    /// </summary>
    public T SanitizeObject<T>(T obj) where T : class
    {
        if (obj == null)
        {
            return obj!;
        }

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        SanitizeObjectRecursive(obj, visited);
        return obj;
    }

    private void SanitizeObjectRecursive(object obj, HashSet<object> visited)
    {
        if (obj == null || !visited.Add(obj))
        {
            // Skip null objects or objects we've already visited (circular reference)
            return;
        }

        var type = obj.GetType();

        // Skip primitive types and strings (strings are handled directly)
        if (type.IsPrimitive || type == typeof(string))
        {
            return;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            var value = property.GetValue(obj);
            if (value == null)
            {
                continue;
            }

            // Sanitize string properties
            if (property.PropertyType == typeof(string))
            {
                var stringValue = value as string;
                if (!string.IsNullOrEmpty(stringValue))
                {
                    property.SetValue(obj, SanitizeInput(stringValue));
                }
            }
            // Handle collections (arrays, lists, etc.)
            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) && 
                     property.PropertyType != typeof(string))
            {
                foreach (var item in (System.Collections.IEnumerable)value)
                {
                    if (item != null && item.GetType().IsClass)
                    {
                        SanitizeObjectRecursive(item, visited);
                    }
                }
            }
            // Recursively sanitize nested objects
            else if (property.PropertyType.IsClass)
            {
                SanitizeObjectRecursive(value, visited);
            }
        }
    }
}
