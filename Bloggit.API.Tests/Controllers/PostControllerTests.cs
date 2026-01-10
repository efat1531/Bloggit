using AutoMapper;
using Bloggit.API.Authorization;
using Bloggit.API.Controller;
using Bloggit.Business.IRepository;
using Bloggit.Data.IServices;
using Bloggit.Data.Models;
using Bloggit.Models.Post;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Bloggit.API.Tests.Controllers;

public class PostControllerTests
{
    private readonly Mock<IPostRepository> _mockPostRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<PostController>> _mockLogger;
    private readonly Mock<IInputSanitizationService> _mockSanitizationService;
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly PostController _controller;
    private const string TestUserId = "test-user-id-123";
    private const string AdminUserId = "admin-user-id-456";

    public PostControllerTests()
    {
        _mockPostRepository = new Mock<IPostRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PostController>>();
        _mockSanitizationService = new Mock<IInputSanitizationService>();
        _mockAuthorizationService = new Mock<IAuthorizationService>();

        _controller = new PostController(
            _mockPostRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockSanitizationService.Object,
            _mockAuthorizationService.Object
        );

        // Setup default HttpContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private void SetupAuthenticatedUser(string userId, bool isAdmin = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, isAdmin ? "Admin" : "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = claimsPrincipal;
    }

    [Fact]
    public async Task Get_ReturnsAllPosts()
    {
        // Arrange
        var posts = new List<Post>
        {
            new() { Id = 1, Title = "Post 1", Content = "Content 1", AuthorId = TestUserId },
            new() { Id = 2, Title = "Post 2", Content = "Content 2", AuthorId = TestUserId }
        };

        var postResponses = new List<PostResponse>
        {
            new() { Id = 1, Title = "Post 1", Content = "Content 1" },
            new() { Id = 2, Title = "Post 2", Content = "Content 2" }
        };

        _mockPostRepository.Setup(x => x.GetPostsAsync()).ReturnsAsync(posts);
        _mockMapper.Setup(x => x.Map<List<PostResponse>>(posts)).Returns(postResponses);

        // Act
        var result = await _controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedPosts = okResult!.Value as List<PostResponse>;
        returnedPosts.Should().HaveCount(2);
        returnedPosts![0].Title.Should().Be("Post 1");
    }

    [Fact]
    public async Task GetPostById_WithValidId_ReturnsPost()
    {
        // Arrange
        var post = new Post
        {
            Id = 1,
            Title = "Test Post",
            Content = "Test Content",
            AuthorId = TestUserId
        };

        var postResponse = new PostResponse
        {
            Id = 1,
            Title = "Test Post",
            Content = "Test Content"
        };

        _mockPostRepository.Setup(x => x.GetPostByIdAsync(1)).ReturnsAsync(post);
        _mockMapper.Setup(x => x.Map<PostResponse>(post)).Returns(postResponse);

        // Act
        var result = await _controller.GetPostById(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedPost = okResult!.Value as PostResponse;
        returnedPost.Should().NotBeNull();
        returnedPost!.Title.Should().Be("Test Post");
    }

    [Fact]
    public async Task GetPostById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockPostRepository.Setup(x => x.GetPostByIdAsync(999)).ReturnsAsync((Post)null!);

        // Act
        var result = await _controller.GetPostById(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedPost()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);

        var createRequest = new CreatePostRequest
        {
            Title = "New Post",
            Content = "New Content"
        };

        var post = new Post
        {
            Id = 1,
            Title = "New Post",
            Content = "New Content",
            AuthorId = TestUserId
        };

        var postResponse = new PostResponse
        {
            Id = 1,
            Title = "New Post",
            Content = "New Content",
            AuthorId = TestUserId
        };

        _mockSanitizationService.Setup(x => x.SanitizeObject(createRequest)).Returns(createRequest);
        _mockMapper.Setup(x => x.Map<Post>(createRequest)).Returns(post);
        _mockPostRepository.Setup(x => x.CreatePostAsync(It.IsAny<Post>())).ReturnsAsync(true);
        _mockMapper.Setup(x => x.Map<PostResponse>(post)).Returns(postResponse);

        // Act
        var result = await _controller.Create(createRequest);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult!.ActionName.Should().Be(nameof(PostController.GetPostById));
        var returnedPost = createdResult.Value as PostResponse;
        returnedPost.Should().NotBeNull();
        returnedPost!.Title.Should().Be("New Post");

        _mockSanitizationService.Verify(x => x.SanitizeObject(createRequest), Times.Once);
        _mockPostRepository.Verify(x => x.CreatePostAsync(It.Is<Post>(p => p.AuthorId == TestUserId)), Times.Once);
    }

    [Fact]
    public async Task Create_SanitizesInput()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);

        var createRequest = new CreatePostRequest
        {
            Title = "Post with <script>alert('XSS')</script>",
            Content = "Content with <script>alert('XSS')</script>"
        };

        var sanitizedRequest = new CreatePostRequest
        {
            Title = "Post with ",
            Content = "Content with "
        };

        _mockSanitizationService.Setup(x => x.SanitizeObject(createRequest)).Returns(sanitizedRequest);
        _mockMapper.Setup(x => x.Map<Post>(It.IsAny<CreatePostRequest>())).Returns(new Post());
        _mockPostRepository.Setup(x => x.CreatePostAsync(It.IsAny<Post>())).ReturnsAsync(true);
        _mockMapper.Setup(x => x.Map<PostResponse>(It.IsAny<Post>())).Returns(new PostResponse());

        // Act
        await _controller.Create(createRequest);

        // Assert
        _mockSanitizationService.Verify(x => x.SanitizeObject(createRequest), Times.Once);
    }

    [Fact]
    public async Task Update_AsAuthor_ReturnsNoContent()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);

        var updateRequest = new UpdatePostRequest
        {
            Title = "Updated Title",
            Content = "Updated Content"
        };

        var existingPost = new Post
        {
            Id = 1,
            Title = "Original Title",
            Content = "Original Content",
            AuthorId = TestUserId
        };

        _mockSanitizationService.Setup(x => x.SanitizeObject(updateRequest)).Returns(updateRequest);
        _mockPostRepository.Setup(x => x.GetPostByIdAsync(1)).ReturnsAsync(existingPost);
        _mockMapper.Setup(x => x.Map(updateRequest, existingPost)).Returns(existingPost);
        _mockPostRepository.Setup(x => x.UpdatePostAsync(existingPost)).ReturnsAsync(true);
        
        // Mock authorization service to succeed (user is author)
        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<Post>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await _controller.Update(1, updateRequest);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockSanitizationService.Verify(x => x.SanitizeObject(updateRequest), Times.Once);
        _mockPostRepository.Verify(x => x.UpdatePostAsync(existingPost), Times.Once);
    }

    [Fact]
    public async Task Update_AsNonAuthor_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser("different-user-id");

        var updateRequest = new UpdatePostRequest
        {
            Title = "Updated Title"
        };

        var existingPost = new Post
        {
            Id = 1,
            Title = "Original Title",
            AuthorId = TestUserId // Owned by different user
        };

        _mockSanitizationService.Setup(x => x.SanitizeObject(updateRequest)).Returns(updateRequest);
        _mockPostRepository.Setup(x => x.GetPostByIdAsync(1)).ReturnsAsync(existingPost);
        
        // Mock authorization service to fail (user is not author or admin)
        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<Post>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Failed());

        // Act
        var result = await _controller.Update(1, updateRequest);

        // Assert
        result.Should().BeOfType<ForbidResult>();
        _mockPostRepository.Verify(x => x.UpdatePostAsync(It.IsAny<Post>()), Times.Never);
    }

    [Fact]
    public async Task Update_WithNonexistentPost_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);

        var updateRequest = new UpdatePostRequest { Title = "Updated Title" };
        _mockSanitizationService.Setup(x => x.SanitizeObject(updateRequest)).Returns(updateRequest);
        _mockPostRepository.Setup(x => x.GetPostByIdAsync(999)).ReturnsAsync((Post)null!);

        // Act
        var result = await _controller.Update(999, updateRequest);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_AsAuthor_ReturnsNoContent()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);

        var existingPost = new Post
        {
            Id = 1,
            Title = "Post to Delete",
            AuthorId = TestUserId
        };

        _mockPostRepository.Setup(x => x.GetPostByIdAsync(1)).ReturnsAsync(existingPost);
        _mockPostRepository.Setup(x => x.DeletePostAsync(1)).ReturnsAsync(true);
        
        // Mock authorization service to succeed (user is author)
        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<Post>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockPostRepository.Verify(x => x.DeletePostAsync(1), Times.Once);
    }

    [Fact]
    public async Task Delete_AsAdmin_ReturnsNoContent()
    {
        // Arrange
        SetupAuthenticatedUser(AdminUserId, isAdmin: true);

        var existingPost = new Post
        {
            Id = 1,
            Title = "Post to Delete",
            AuthorId = TestUserId // Owned by different user
        };

        _mockPostRepository.Setup(x => x.GetPostByIdAsync(1)).ReturnsAsync(existingPost);
        _mockPostRepository.Setup(x => x.DeletePostAsync(1)).ReturnsAsync(true);
        
        // Mock authorization service to succeed (user is admin)
        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<Post>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockPostRepository.Verify(x => x.DeletePostAsync(1), Times.Once);
    }

    [Fact]
    public async Task Delete_AsNonAuthorAndNonAdmin_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser("different-user-id");

        var existingPost = new Post
        {
            Id = 1,
            Title = "Post to Delete",
            AuthorId = TestUserId // Owned by different user
        };

        _mockPostRepository.Setup(x => x.GetPostByIdAsync(1)).ReturnsAsync(existingPost);
        
        // Mock authorization service to fail (user is not author or admin)
        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<Post>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Failed());

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<ForbidResult>();
        _mockPostRepository.Verify(x => x.DeletePostAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WithNonexistentPost_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);
        _mockPostRepository.Setup(x => x.GetPostByIdAsync(999)).ReturnsAsync((Post)null!);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);
        var createRequest = new CreatePostRequest();
        _controller.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await _controller.Create(createRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _mockPostRepository.Verify(x => x.CreatePostAsync(It.IsAny<Post>()), Times.Never);
    }

    [Fact]
    public async Task Update_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);
        var updateRequest = new UpdatePostRequest();
        _controller.ModelState.AddModelError("Title", "Invalid title");

        // Act
        var result = await _controller.Update(1, updateRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _mockPostRepository.Verify(x => x.UpdatePostAsync(It.IsAny<Post>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenRepositoryFails_ReturnsInternalServerError()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);

        var createRequest = new CreatePostRequest
        {
            Title = "Test Post",
            Content = "Test Content"
        };

        _mockSanitizationService.Setup(x => x.SanitizeObject(createRequest)).Returns(createRequest);
        _mockMapper.Setup(x => x.Map<Post>(createRequest)).Returns(new Post());
        _mockPostRepository.Setup(x => x.CreatePostAsync(It.IsAny<Post>())).ReturnsAsync(false);

        // Act
        var result = await _controller.Create(createRequest);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Update_WhenRepositoryFails_ReturnsInternalServerError()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);

        var updateRequest = new UpdatePostRequest { Title = "Updated" };
        var existingPost = new Post { Id = 1, AuthorId = TestUserId };

        _mockSanitizationService.Setup(x => x.SanitizeObject(updateRequest)).Returns(updateRequest);
        _mockPostRepository.Setup(x => x.GetPostByIdAsync(1)).ReturnsAsync(existingPost);
        _mockMapper.Setup(x => x.Map(updateRequest, existingPost)).Returns(existingPost);
        _mockPostRepository.Setup(x => x.UpdatePostAsync(existingPost)).ReturnsAsync(false);
        
        // Mock authorization service to succeed
        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<Post>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await _controller.Update(1, updateRequest);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Delete_WhenRepositoryFails_ReturnsInternalServerError()
    {
        // Arrange
        SetupAuthenticatedUser(TestUserId);
        var existingPost = new Post { Id = 1, AuthorId = TestUserId };

        _mockPostRepository.Setup(x => x.GetPostByIdAsync(1)).ReturnsAsync(existingPost);
        _mockPostRepository.Setup(x => x.DeletePostAsync(1)).ReturnsAsync(false);
        
        // Mock authorization service to succeed
        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(), 
                It.IsAny<Post>(), 
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }
}
