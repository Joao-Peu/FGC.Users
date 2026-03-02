using Xunit;
using FluentAssertions;
using FluentValidation.TestHelper;
using FGC.Users.Application.Validators;
using FGC.Users.Tests.Fixtures;

namespace FGC.Users.Tests.Validators;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator;

    public RegisterValidatorTests()
    {
        _validator = new RegisterValidator();
    }

    #region Email Validation Tests

    [Fact]
    public void Validate_WithValidEmail_ShouldPass()
    {
        // Arrange
        var request = TestData.Requests.ValidRegisterRequest;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldFail()
    {
        // Arrange
        var request = new FGC.Users.Application.DTOs.RegisterRequest(
            Email: string.Empty,
            Password: "ValidPassword123!",
            FullName: "Test User"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithInvalidEmailFormat_ShouldFail()
    {
        // Arrange
        var request = TestData.Requests.InvalidEmailRequest;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@company.co.uk")]
    [InlineData("support+tag@domain.org")]
    public void Validate_WithVariousValidEmails_ShouldPass(string email)
    {
        // Arrange
        var request = new FGC.Users.Application.DTOs.RegisterRequest(
            Email: email,
            Password: "ValidPassword123!",
            FullName: "Test User"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Password Validation Tests

    [Fact]
    public void Validate_WithValidPassword_ShouldPass()
    {
        // Arrange
        var request = TestData.Requests.ValidRegisterRequest;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithEmptyPassword_ShouldFail()
    {
        // Arrange
        var request = new FGC.Users.Application.DTOs.RegisterRequest(
            Email: "test@example.com",
            Password: string.Empty,
            FullName: "Test User"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithShortPassword_ShouldFail()
    {
        // Arrange
        var request = TestData.Requests.ShortPasswordRequest;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("password")]
    [InlineData("Pass123")]
    public void Validate_WithPasswordsMinimumLength_ShouldPass(string password)
    {
        // Arrange
        var request = new FGC.Users.Application.DTOs.RegisterRequest(
            Email: "test@example.com",
            Password: password,
            FullName: "Test User"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Validate_WithAllValidData_ShouldPass()
    {
        // Arrange
        var request = TestData.Requests.ValidRegisterRequest;

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMultipleInvalidFields_ShouldFailForEachField()
    {
        // Arrange
        var request = new FGC.Users.Application.DTOs.RegisterRequest(
            Email: "invalid-email",
            Password: "123",
            FullName: "Test User"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    #endregion
}
