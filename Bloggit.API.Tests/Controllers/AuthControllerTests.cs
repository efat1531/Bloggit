using AutoMapper;
using Bloggit.API.Controller;
using Bloggit.Business.IRepository;
using Bloggit.Data.Configuration;
using Bloggit.Data.IServices;
using Bloggit.Data.Models;
using Bloggit.Models.Auth;
using Bloggit.Models.User;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Bloggit.API.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly Mock<IOptions<JwtSettings>> _mockJwtSettings;
    private readonly Mock<IInputSanitizationService> _mockSanitizationService;
    private readonly AuthController _controller;
    private readonly JwtSettings _jwtSettings;

    public AuthControllerTests()
    {
        // Setup UserManager mock
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null, null, null, null, null, null, null, null);

        // Setup SignInManager mock
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            _mockUserManager.Object,
            contextAccessor.Object,
            userPrincipalFactory.Object,
            null, null, null, null);

        _mockAuthService = new Mock<IAuthService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _mockSanitizationService = new Mock<IInputSanitizationService>();

        // Setup JWT settings
        // NOTE: This is a test-only JWT secret for unit tests, not a production secret
        _jwtSettings = new JwtSettings
        {
            Secret = "test-only-secret-key-for-unit-tests-not-used-in-production-min-32-chars",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationHours = 24
        };
        _mockJwtSettings = new Mock<IOptions<JwtSettings>>();
        _mockJwtSettings.Setup(x => x.Value).Returns(_jwtSettings);

        // Create controller
        _controller = new AuthController(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockAuthService.Object,
            _mockEmailService.Object,
            _mockUserRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _mockJwtSettings.Object,
            _mockSanitizationService.Object
        );

        // Setup HttpContext for cookie operations
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsOk()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "Test@123",
            FirstName = "Test",
            LastName = "User"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(registerRequest.Email))
            .ReturnsAsync((ApplicationUser)null!);
        _mockUserManager.Setup(x => x.FindByNameAsync(registerRequest.Username))
            .ReturnsAsync((ApplicationUser)null!);

        var user = new ApplicationUser { Email = registerRequest.Email, UserName = registerRequest.Username };
        _mockMapper.Setup(x => x.Map<ApplicationUser>(registerRequest)).Returns(user);

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerRequest.Password))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("test-token");
        _mockUserManager.Setup(x => x.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), "test-token"))
            .ReturnsAsync(IdentityResult.Success);

        _mockSanitizationService.Setup(x => x.SanitizeObject(registerRequest)).Returns(registerRequest);

        // Act
        var result = await _controller.RegisterAsync(registerRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();

        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerRequest.Password), Times.Once);
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "existing@example.com",
            Username = "testuser",
            Password = "Test@123"
        };

        var existingUser = new ApplicationUser { Email = registerRequest.Email };
        _mockUserManager.Setup(x => x.FindByEmailAsync(registerRequest.Email))
            .ReturnsAsync(existingUser);

        _mockSanitizationService.Setup(x => x.SanitizeObject(registerRequest)).Returns(registerRequest);

        // Act
        var result = await _controller.RegisterAsync(registerRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            Username = "existinguser",
            Password = "Test@123"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(registerRequest.Email))
            .ReturnsAsync((ApplicationUser)null!);

        var existingUser = new ApplicationUser { UserName = registerRequest.Username };
        _mockUserManager.Setup(x => x.FindByNameAsync(registerRequest.Username))
            .ReturnsAsync(existingUser);

        _mockSanitizationService.Setup(x => x.SanitizeObject(registerRequest)).Returns(registerRequest);

        // Act
        var result = await _controller.RegisterAsync(registerRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrUsername = "test@example.com",
            Password = "Test@123"
        };

        var user = new ApplicationUser
        {
            Id = "user-id-123",
            Email = "test@example.com",
            UserName = "testuser"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginRequest.EmailOrUsername))
            .ReturnsAsync(user);
        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, loginRequest.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });
        _mockUserManager.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(new List<System.Security.Claims.Claim>());

        var token = "test-jwt-token";
        _mockAuthService.Setup(x => x.GenerateJwtToken(user, It.IsAny<IList<string>>(), It.IsAny<IList<System.Security.Claims.Claim>>()))
            .Returns(token);
        _mockAuthService.Setup(x => x.GetTokenExpiration(_jwtSettings.ExpirationHours))
            .Returns(DateTime.UtcNow.AddHours(24));

        var userResponse = new UserResponse { Id = user.Id, Email = user.Email, Username = user.UserName };
        _mockMapper.Setup(x => x.Map<UserResponse>(user)).Returns(userResponse);

        // Act
        var result = await _controller.LoginAsync(loginRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var authResponse = okResult!.Value as AuthResponse;
        authResponse.Should().NotBeNull();
        authResponse!.Token.Should().Be(token);
        authResponse.User.Should().Be(userResponse);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidUser_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrUsername = "nonexistent@example.com",
            Password = "Test@123"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginRequest.EmailOrUsername))
            .ReturnsAsync((ApplicationUser)null!);
        _mockUserManager.Setup(x => x.FindByNameAsync(loginRequest.EmailOrUsername))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _controller.LoginAsync(loginRequest);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrUsername = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new ApplicationUser { Email = "test@example.com" };
        _mockUserManager.Setup(x => x.FindByEmailAsync(loginRequest.EmailOrUsername))
            .ReturnsAsync(user);
        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, loginRequest.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.LoginAsync(loginRequest);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task LoginAsync_WithLockedAccount_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            EmailOrUsername = "test@example.com",
            Password = "Test@123"
        };

        var user = new ApplicationUser { Email = "test@example.com" };
        _mockUserManager.Setup(x => x.FindByEmailAsync(loginRequest.EmailOrUsername))
            .ReturnsAsync(user);
        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, loginRequest.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        // Act
        var result = await _controller.LoginAsync(loginRequest);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidData_ReturnsOk()
    {
        // Arrange
        var changePasswordRequest = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword@123",
            NewPassword = "NewPassword@123"
        };

        var user = new ApplicationUser
        {
            Id = "user-id-123",
            Email = "test@example.com"
        };

        // Setup authenticated user context
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id)
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = claimsPrincipal;

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ChangePasswordAsync(user, changePasswordRequest.CurrentPassword, changePasswordRequest.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.ChangePasswordAsync(changePasswordRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockUserManager.Verify(x => x.ChangePasswordAsync(user, changePasswordRequest.CurrentPassword, changePasswordRequest.NewPassword), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ReturnsBadRequest()
    {
        // Arrange
        var changePasswordRequest = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword@123"
        };

        var user = new ApplicationUser { Id = "user-id-123" };

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id)
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext.User = claimsPrincipal;

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ChangePasswordAsync(user, changePasswordRequest.CurrentPassword, changePasswordRequest.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password" }));

        // Act
        var result = await _controller.ChangePasswordAsync(changePasswordRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithValidToken_ReturnsOk()
    {
        // Arrange
        var userId = "user-id-123";
        var token = "valid-token";
        var user = new ApplicationUser { Id = userId, Email = "test@example.com" };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ConfirmEmailAsync(user, token))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.ConfirmEmailAsync(userId, token);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var userId = "user-id-123";
        var token = "invalid-token";
        var user = new ApplicationUser { Id = userId };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ConfirmEmailAsync(user, token))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        // Act
        var result = await _controller.ConfirmEmailAsync(userId, token);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithNonexistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = "nonexistent-user";
        var token = "some-token";

        _mockUserManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _controller.ConfirmEmailAsync(userId, token);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithEmptyParameters_ReturnsBadRequest()
    {
        // Arrange & Act
        var result = await _controller.ConfirmEmailAsync("", "");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
