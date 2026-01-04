using AutoMapper;
using Bloggit.API.Controller;
using Bloggit.Business.IRepository;
using Bloggit.Data.Models;
using Bloggit.Models.User;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Bloggit.API.Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<UserController>> _mockLogger;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        // Setup UserManager mock
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null, null, null, null, null, null, null, null);

        _mockUserRepository = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<UserController>>();

        _controller = new UserController(
            _mockUserManager.Object,
            _mockUserRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetUsersAsync_WithNoRoleFilter_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "user1", Email = "user1@test.com" },
            new() { Id = "2", UserName = "user2", Email = "user2@test.com" }
        };

        _mockUserRepository.Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(users);

        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });

        var userDto1 = new UserWithRolesResponse { Id = "1", Username = "user1", Email = "user1@test.com" };
        var userDto2 = new UserWithRolesResponse { Id = "2", Username = "user2", Email = "user2@test.com" };
        _mockMapper.Setup(x => x.Map<UserWithRolesResponse>(users[0])).Returns(userDto1);
        _mockMapper.Setup(x => x.Map<UserWithRolesResponse>(users[1])).Returns(userDto2);

        // Act
        var result = await _controller.GetUsersAsync();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedUsers = okResult!.Value as List<UserWithRolesResponse>;
        returnedUsers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUsersAsync_WithAdminRoleFilter_ReturnsOnlyAdmins()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "admin1", Email = "admin1@test.com" },
            new() { Id = "2", UserName = "user1", Email = "user1@test.com" }
        };

        _mockUserRepository.Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(users);

        _mockUserManager.Setup(x => x.GetRolesAsync(users[0]))
            .ReturnsAsync(new List<string> { "Admin" });
        _mockUserManager.Setup(x => x.GetRolesAsync(users[1]))
            .ReturnsAsync(new List<string> { "User" });

        var userDto1 = new UserWithRolesResponse { Id = "1", Username = "admin1", Email = "admin1@test.com" };
        _mockMapper.Setup(x => x.Map<UserWithRolesResponse>(users[0])).Returns(userDto1);

        // Act
        var result = await _controller.GetUsersAsync("Admin");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedUsers = okResult!.Value as List<UserWithRolesResponse>;
        returnedUsers.Should().HaveCount(1);
        returnedUsers![0].Username.Should().Be("admin1");
    }

    [Fact]
    public async Task PromoteUserAsync_WithValidUser_ReturnsOk()
    {
        // Arrange
        var request = new ManageUserRoleRequest { UserId = "user-123" };
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };

        _mockUserManager.Setup(x => x.FindByIdAsync(request.UserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(false);
        _mockUserManager.Setup(x => x.AddToRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.PromoteUserAsync(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockUserManager.Verify(x => x.AddToRoleAsync(user, "Admin"), Times.Once);
    }

    [Fact]
    public async Task PromoteUserAsync_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var request = new ManageUserRoleRequest { UserId = "invalid-user" };

        _mockUserManager.Setup(x => x.FindByIdAsync(request.UserId))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _controller.PromoteUserAsync(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task PromoteUserAsync_WithExistingAdmin_ReturnsBadRequest()
    {
        // Arrange
        var request = new ManageUserRoleRequest { UserId = "admin-123" };
        var user = new ApplicationUser { Id = "admin-123", UserName = "adminuser" };

        _mockUserManager.Setup(x => x.FindByIdAsync(request.UserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.PromoteUserAsync(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DemoteUserAsync_WithValidAdmin_ReturnsOk()
    {
        // Arrange
        var request = new ManageUserRoleRequest { UserId = "admin-123" };
        var user = new ApplicationUser { Id = "admin-123", UserName = "adminuser" };

        _mockUserManager.Setup(x => x.FindByIdAsync(request.UserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(new List<Claim>());
        _mockUserManager.Setup(x => x.RemoveFromRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.DemoteUserAsync(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockUserManager.Verify(x => x.RemoveFromRoleAsync(user, "Admin"), Times.Once);
    }

    [Fact]
    public async Task DemoteUserAsync_WithNonAdmin_ReturnsBadRequest()
    {
        // Arrange
        var request = new ManageUserRoleRequest { UserId = "user-123" };
        var user = new ApplicationUser { Id = "user-123", UserName = "regularuser" };

        _mockUserManager.Setup(x => x.FindByIdAsync(request.UserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DemoteUserAsync(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DemoteUserAsync_WithSuperAdmin_ReturnsBadRequest()
    {
        // Arrange
        var request = new ManageUserRoleRequest { UserId = "superadmin-123" };
        var user = new ApplicationUser { Id = "superadmin-123", UserName = "superadmin" };

        _mockUserManager.Setup(x => x.FindByIdAsync(request.UserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(new List<Claim> { new Claim("SuperAdmin", "true") });

        // Act
        var result = await _controller.DemoteUserAsync(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignSuperAdminAsync_WithValidUser_ReturnsOk()
    {
        // Arrange
        var request = new ManageUserRoleRequest { UserId = "user-123" };
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };

        _mockUserManager.Setup(x => x.FindByIdAsync(request.UserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(new List<Claim>());
        _mockUserManager.Setup(x => x.AddClaimAsync(user, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.IsInRoleAsync(user, "Admin"))
            .ReturnsAsync(false);
        _mockUserManager.Setup(x => x.AddToRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.AssignSuperAdminAsync(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockUserManager.Verify(x => x.AddClaimAsync(user, It.Is<Claim>(c => c.Type == "SuperAdmin" && c.Value == "true")), Times.Once);
    }

    [Fact]
    public async Task AssignSuperAdminAsync_WithExistingSuperAdmin_ReturnsBadRequest()
    {
        // Arrange
        var request = new ManageUserRoleRequest { UserId = "superadmin-123" };
        var user = new ApplicationUser { Id = "superadmin-123", UserName = "superadmin" };

        _mockUserManager.Setup(x => x.FindByIdAsync(request.UserId))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetClaimsAsync(user))
            .ReturnsAsync(new List<Claim> { new Claim("SuperAdmin", "true") });

        // Act
        var result = await _controller.AssignSuperAdminAsync(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
