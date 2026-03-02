using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using FGC.Users.Application.Services;
using FGC.Users.Application.DTOs;
using FGC.Users.Application.Interfaces;
using FGC.Users.Domain.Entities;
using FGC.Users.Infrastructure;
using FGC.Users.Tests.Fixtures;
using Microsoft.Extensions.Configuration;

namespace FGC.Users.Tests.Services;

public class UserServiceTests : IAsyncLifetime
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        // Criar DbContext em memória
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mockAuditService = new Mock<IAuditService>();
        _mockEventPublisher = new Mock<IEventPublisher>();

        // Criar uma configuração real com valores de teste
        var configDict = new Dictionary<string, string?> 
        {
            {"Jwt:Issuer", "fgc.local"},
            {"Jwt:Audience", "fgc.clients"},
            {"Jwt:Secret", "your-super-secret-key-that-is-at-least-32-characters-long!!"}
        };

        var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(configDict);

        var configuration = configBuilder.Build();

        _userService = new UserService(
            _dbContext,
            _mockAuditService.Object,
            _mockEventPublisher.Object,
            configuration
        );
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ShouldCreateUser()
    {
        // Arrange
        var request = TestData.Requests.ValidRegisterRequest;
        var correlationId = TestData.CorrelationIds.Valid;

        // Act
        var result = await _userService.RegisterAsync(request, correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.FullName.Should().Be(request.FullName);
        
        var userInDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        userInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = TestData.Requests.ValidRegisterRequest;
        var correlationId = TestData.CorrelationIds.Valid;

        // Criar primeiro usuário
        await _userService.RegisterAsync(request, correlationId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.RegisterAsync(request, correlationId)
        );
    }

    [Fact]
    public async Task RegisterAsync_WhenSuccess_ShouldAuditAndPublishEvent()
    {
        // Arrange
        var request = TestData.Requests.ValidRegisterRequest;
        var correlationId = TestData.CorrelationIds.Valid;

        // Act
        await _userService.RegisterAsync(request, correlationId);

        // Assert
        _mockAuditService.Verify(
            x => x.AuditAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                "UserRegistered",
                null,
                It.IsAny<User>(),
                correlationId,
                null
            ),
            Times.Once
        );

        _mockEventPublisher.Verify(
            x => x.PublishAsync(
                "UserRegistered",
                It.IsAny<object>(),
                correlationId
            ),
            Times.Once
        );
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        var user = TestData.Users.ValidUser;
        var request = new LoginRequest(user.Email, "password123");
        var correlationId = TestData.CorrelationIds.Valid;

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.LoginAsync(request, correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "password123");
        var correlationId = TestData.CorrelationIds.Valid;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.LoginAsync(request, correlationId)
        );
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = TestData.Users.ValidUser;
        var request = new LoginRequest(user.Email, "wrongpassword");
        var correlationId = TestData.CorrelationIds.Valid;

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.LoginAsync(request, correlationId)
        );
    }

    #endregion

    #region GetMeAsync Tests

    [Fact]
    public async Task GetMeAsync_WithValidUserId_ShouldReturnUser()
    {
        // Arrange
        var user = TestData.Users.ValidUser;
        var userId = user.Id.ToString();

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.GetMeAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.FullName.Should().Be(user.FullName);
    }

    [Fact]
    public async Task GetMeAsync_WithInvalidUserId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _userService.GetMeAsync(userId)
        );
    }

    #endregion

    #region UpdateMeAsync Tests

    [Fact]
    public async Task UpdateMeAsync_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var user = TestData.Users.ValidUser;
        var userId = user.Id.ToString();
        var request = new RegisterRequest(
            Email: user.Email,
            Password: "newpassword123",
            FullName: "Updated Name"
        );
        var correlationId = TestData.CorrelationIds.Valid;

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _userService.UpdateMeAsync(userId, request, correlationId);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be(request.FullName);
        
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FullName.Should().Be(request.FullName);
    }

    [Fact]
    public async Task UpdateMeAsync_WithNonexistentUser_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var request = TestData.Requests.ValidRegisterRequest;
        var correlationId = TestData.CorrelationIds.Valid;

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _userService.UpdateMeAsync(userId, request, correlationId)
        );
    }

    [Fact]
    public async Task UpdateMeAsync_WhenSuccess_ShouldAuditAndPublishEvent()
    {
        // Arrange
        var user = TestData.Users.ValidUser;
        var userId = user.Id.ToString();
        var request = new RegisterRequest(
            Email: user.Email,
            Password: null,
            FullName: "Updated Name"
        );
        var correlationId = TestData.CorrelationIds.Valid;

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        await _userService.UpdateMeAsync(userId, request, correlationId);

        // Assert
        _mockAuditService.Verify(
            x => x.AuditAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                "UserProfileUpdated",
                It.IsAny<object>(),
                It.IsAny<User>(),
                correlationId,
                userId
            ),
            Times.Once
        );

        _mockEventPublisher.Verify(
            x => x.PublishAsync(
                "UserProfileUpdated",
                It.IsAny<object>(),
                correlationId
            ),
            Times.Once
        );
    }

    #endregion
}

