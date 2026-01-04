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

        // Configure allowed tags for rich text content
        // This is an EXPLICIT ALLOWLIST - only these tags are permitted, all others are removed
        _sanitizer.AllowedTags.Clear();
        
        // Text formatting and semantic tags
        _sanitizer.AllowedTags.Add("p");        // Paragraphs
        _sanitizer.AllowedTags.Add("br");       // Line breaks
        _sanitizer.AllowedTags.Add("div");      // Generic containers (safe when attributes are sanitized)
        _sanitizer.AllowedTags.Add("strong");   // Bold/important text
        _sanitizer.AllowedTags.Add("em");       // Italic/emphasized text
        _sanitizer.AllowedTags.Add("u");        // Underlined text
        
        // Headings
        _sanitizer.AllowedTags.Add("h1");
        _sanitizer.AllowedTags.Add("h2");
        _sanitizer.AllowedTags.Add("h3");
        _sanitizer.AllowedTags.Add("h4");
        _sanitizer.AllowedTags.Add("h5");
        _sanitizer.AllowedTags.Add("h6");
        
        // Lists
        _sanitizer.AllowedTags.Add("ul");       // Unordered lists
        _sanitizer.AllowedTags.Add("ol");       // Ordered lists
        _sanitizer.AllowedTags.Add("li");       // List items
        
        // Other content
        _sanitizer.AllowedTags.Add("a");        // Links (href sanitized below)
        _sanitizer.AllowedTags.Add("blockquote"); // Quotations
        _sanitizer.AllowedTags.Add("code");     // Inline code
        _sanitizer.AllowedTags.Add("pre");      // Preformatted text/code blocks
        
        // DISALLOWED tags (removed by sanitizer):
        // - script, style, iframe, object, embed: Executable content
        // - form, input, button, select, textarea: Form elements
        // - img, video, audio: Media (can be added later if needed with proper src validation)
        // - meta, link, base: Document metadata
        // - Any other HTML tags not explicitly listed above

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
