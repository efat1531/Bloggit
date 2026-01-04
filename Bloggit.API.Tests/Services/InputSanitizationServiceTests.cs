using Bloggit.Data.Services;
using FluentAssertions;

namespace Bloggit.API.Tests.Services;

public class InputSanitizationServiceTests
{
    private readonly InputSanitizationService _sanitizationService;

    public InputSanitizationServiceTests()
    {
        _sanitizationService = new InputSanitizationService();
    }

    [Fact]
    public void SanitizeInput_WithScriptTag_RemovesScript()
    {
        // Arrange
        var input = "Hello <script>alert('XSS')</script> World";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("alert");
        result.Should().Contain("Hello");
        result.Should().Contain("World");
    }

    [Fact]
    public void SanitizeInput_WithOnEventHandler_RemovesHandler()
    {
        // Arrange
        var input = "<div onclick='alert(123)'>Click me</div>";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().NotContain("onclick");
        result.Should().NotContain("alert");
        result.Should().Contain("<div>");
        result.Should().Contain("Click me");
        result.Should().Contain("</div>");
    }

    [Fact]
    public void SanitizeInput_WithIframe_RemovesIframe()
    {
        // Arrange
        var input = "Content <iframe src='http://evil.com'></iframe> more content";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().NotContain("<iframe");
        result.Should().NotContain("evil.com");
    }

    [Fact]
    public void SanitizeInput_WithStyleTag_RemovesStyle()
    {
        // Arrange
        var input = "Text <style>body{display:none;}</style> more text";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().NotContain("<style>");
        result.Should().NotContain("display:none");
    }

    [Fact]
    public void SanitizeInput_WithJavaScriptProtocol_RemovesProtocol()
    {
        // Arrange
        var input = "<a href='javascript:alert(1)'>Click</a>";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().NotContain("javascript:");
        result.Should().NotContain("alert");
    }

    [Fact]
    public void SanitizeInput_AllowsSafeTags()
    {
        // Arrange
        var input = "<p>Paragraph</p><br><strong>Bold</strong><em>Italic</em>";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().Contain("<p>");
        result.Should().Contain("Paragraph");
        result.Should().Contain("</p>");
        result.Should().Contain("<br");
        result.Should().Contain("<strong>");
        result.Should().Contain("<em>");
    }

    [Fact]
    public void SanitizeInput_WithNullInput_ReturnsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = _sanitizationService.SanitizeInput(input!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SanitizeInput_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var input = "";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeInput_WithPlainText_ReturnsUnchanged()
    {
        // Arrange
        var input = "This is plain text without any HTML";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void SanitizeObject_SanitizesStringProperties()
    {
        // Arrange
        var testObject = new TestModel
        {
            Title = "Title <script>alert('XSS')</script>",
            Content = "Content <iframe src='evil.com'></iframe>",
            Number = 42
        };

        // Act
        var result = _sanitizationService.SanitizeObject(testObject);

        // Assert
        result.Title.Should().NotContain("<script>");
        result.Title.Should().Contain("Title");
        result.Content.Should().NotContain("<iframe");
        result.Content.Should().Contain("Content");
        result.Number.Should().Be(42); // Non-string properties unchanged
    }

    [Fact]
    public void SanitizeObject_WithNullObject_ReturnsNull()
    {
        // Arrange
        TestModel? testObject = null;

        // Act
        var result = _sanitizationService.SanitizeObject(testObject!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SanitizeObject_OnlySanitizesTopLevelStringProperties()
    {
        // Arrange
        var testObject = new NestedTestModel
        {
            Name = "Name <script>alert(1)</script>",
            Inner = new TestModel
            {
                Title = "Title <script>alert(2)</script>",
                Content = "Content"
            }
        };

        // Act
        var result = _sanitizationService.SanitizeObject(testObject);

        // Assert
        result.Name.Should().NotContain("<script>");
        // Note: SanitizeObject only sanitizes direct string properties, not nested objects
        // For nested objects, you need to call SanitizeObject on each level
        result.Inner.Title.Should().Contain("<script>"); // Not sanitized at nested level
    }

    [Fact]
    public void SanitizeInput_WithSqlInjectionAttempt_SanitizesContent()
    {
        // Arrange
        var input = "User'; DROP TABLE Users; --";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        // The sanitizer should return the text as-is since it's not HTML
        // SQL injection protection should be at the database level with parameterized queries
        result.Should().Be(input);
    }

    [Fact]
    public void SanitizeInput_WithMultipleScriptTags_RemovesAll()
    {
        // Arrange
        var input = "<script>alert(1)</script>Text<script>alert(2)</script>";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("alert");
        result.Should().Contain("Text");
    }

    [Fact]
    public void SanitizeInput_WithDisallowedTags_RemovesEntireTag()
    {
        // Arrange
        var input = "<section data-value='safe' onclick='unsafe()'>Content</section>";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        // section is not in the allowed tags list, so entire tag is removed
        result.Should().NotContain("onclick");
        result.Should().NotContain("section");
        result.Should().NotContain("data-value");
        // Content inside disallowed tags may be removed depending on sanitizer behavior
    }

    [Fact]
    public void SanitizeInput_WithDivTag_PreservesDivButRemovesDangerousAttributes()
    {
        // Arrange
        var input = "<div onclick='alert(123)' class='danger'>Safe Content</div>";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        // div is in the allowed tags list, so tag is preserved
        result.Should().Contain("<div>");
        result.Should().Contain("Safe Content");
        result.Should().Contain("</div>");
        // But dangerous attributes are removed
        result.Should().NotContain("onclick");
        result.Should().NotContain("alert");
        result.Should().NotContain("class"); // class is not in allowed attributes
    }

    [Fact]
    public void SanitizeInput_WithEncodedScript_KeepsEncoded()
    {
        // Arrange
        var input = "Text &lt;script&gt;alert('XSS')&lt;/script&gt;";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        // HTML entities are already safe - they display as text, not executed as code
        // The sanitizer preserves them as they are not dangerous
        result.Should().Contain("&lt;");
        result.Should().Contain("&gt;");
    }

    [Fact]
    public void SanitizeInput_WithAllowedLinks_PreservesLinks()
    {
        // Arrange
        var input = "<a href='https://example.com'>Visit Example</a>";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().Contain("<a");
        result.Should().Contain("href");
        result.Should().Contain("https://example.com");
        result.Should().Contain("Visit Example");
    }

    [Fact]
    public void SanitizeInput_WithFormElements_RemovesForms()
    {
        // Arrange
        var input = "<form action='/malicious'><input type='text'></form>";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().NotContain("<form");
        result.Should().NotContain("<input");
        result.Should().NotContain("malicious");
    }

    [Fact]
    public void SanitizeInput_WithObjectAndEmbed_RemovesTags()
    {
        // Arrange
        var input = "<object data='malicious.swf'></object><embed src='bad.swf'>";

        // Act
        var result = _sanitizationService.SanitizeInput(input);

        // Assert
        result.Should().NotContain("<object");
        result.Should().NotContain("<embed");
    }

    // Test helper classes
    private class TestModel
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Number { get; set; }
    }

    private class NestedTestModel
    {
        public string Name { get; set; } = string.Empty;
        public TestModel Inner { get; set; } = new();
    }
}
