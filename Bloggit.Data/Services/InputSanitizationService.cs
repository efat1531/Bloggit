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

    public T SanitizeObject<T>(T obj) where T : class
    {
        if (obj == null)
        {
            return obj!;
        }

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            var value = property.GetValue(obj) as string;
            if (!string.IsNullOrEmpty(value))
            {
                property.SetValue(obj, SanitizeInput(value));
            }
        }

        return obj;
    }
}
