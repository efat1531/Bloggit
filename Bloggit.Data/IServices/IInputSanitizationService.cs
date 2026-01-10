namespace Bloggit.Data.IServices;

/// <summary>
/// Service for sanitizing user input to prevent XSS and other injection attacks
/// </summary>
public interface IInputSanitizationService
{
    /// <summary>
    /// Sanitizes a string by removing potentially dangerous HTML/JavaScript content
    /// </summary>
    string SanitizeInput(string input);

    /// <summary>
    /// Recursively sanitizes all string properties of an object and its nested objects.
    /// Handles circular references to prevent infinite loops.
    /// </summary>
    T SanitizeObject<T>(T obj) where T : class;
}
