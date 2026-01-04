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
    /// Sanitizes all string properties of an object
    /// </summary>
    T SanitizeObject<T>(T obj) where T : class;
}
