using ClassmateApi.Controllers;
using ClassmateApi.DTOs;
using ClassmateApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ClassmateApi.Tests.Controllers;

/// <summary>
/// Unit tests for AuthController.
/// UserService is mocked — we're testing the HTTP layer (routing, status codes, model binding).
/// </summary>
public class AuthControllerTests
{
    private static AuthController CreateController(IUserService? userService = null)
    {
        var svc = userService ?? Mock.Of<IUserService>();
        return new AuthController(svc, NullLogger<AuthController>.Instance);
    }

    private static GoogleAuthRequest ValidRequest() =>
        new("valid-id-token", "valid-access-token", "valid-refresh-token");

    private static AuthResponse FakeAuthResponse() =>
        new("jwt.token.here", new UserDto(1, "a@b.com", "Alice", null, 5, false, false, "a@b.com"));

    // ─── POST /api/auth/google ─────────────────────────────────────────────────

    [Fact]
    public async Task GoogleAuth_ValidRequest_Returns200WithToken()
    {
        // Arrange
        var mockSvc = new Mock<IUserService>();
        mockSvc.Setup(s => s.AuthenticateWithGoogleAsync(It.IsAny<GoogleAuthRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(FakeAuthResponse());

        var controller = CreateController(mockSvc.Object);

        // Act
        var result = await controller.GoogleAuth(ValidRequest(), CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);

        var body = ok.Value.Should().BeOfType<AuthResponse>().Subject;
        body.Token.Should().Be("jwt.token.here");
        body.User.Email.Should().Be("a@b.com");
    }

    [Fact]
    public async Task GoogleAuth_InvalidGoogleToken_Returns401()
    {
        // Arrange
        var mockSvc = new Mock<IUserService>();
        mockSvc.Setup(s => s.AuthenticateWithGoogleAsync(It.IsAny<GoogleAuthRequest>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new UnauthorizedAccessException("Invalid Google ID token."));

        var controller = CreateController(mockSvc.Object);

        // Act
        var result = await controller.GoogleAuth(ValidRequest(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>()
              .Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GoogleAuth_ServiceCalledOnce_WithCorrectArguments()
    {
        // Arrange
        var mockSvc = new Mock<IUserService>();
        mockSvc.Setup(s => s.AuthenticateWithGoogleAsync(It.IsAny<GoogleAuthRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(FakeAuthResponse());

        var controller  = CreateController(mockSvc.Object);
        var req         = ValidRequest();

        // Act
        await controller.GoogleAuth(req, CancellationToken.None);

        // Assert — service was called exactly once with our request
        mockSvc.Verify(
            s => s.AuthenticateWithGoogleAsync(
                It.Is<GoogleAuthRequest>(r =>
                    r.IdToken      == req.IdToken &&
                    r.RefreshToken == req.RefreshToken),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GoogleAuth_EmptyIdToken_ModelStateInvalid_Returns400()
    {
        // Arrange
        var controller = CreateController();
        // Simulate model binding failure (as ASP.NET would do with [Required])
        controller.ModelState.AddModelError("IdToken", "The IdToken field is required.");

        // Act
        var result = await controller.GoogleAuth(
            new GoogleAuthRequest("", "access", "refresh"),
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.StatusCode.Should().Be(400);
    }
}