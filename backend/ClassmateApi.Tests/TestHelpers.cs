using ClassmateApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClassmateApi.Tests.Helpers;

/// <summary>
/// Builds an in-memory AppDbContext for unit tests.
/// Each call gets a uniquely named database so tests don't bleed into each other.
/// </summary>
public static class DbContextFactory
{
    public static AppDbContext CreateInMemory(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}

/// <summary>
/// Creates valid JWTs for use in unit / integration tests.
/// Uses the same algorithm as UserService so tokens pass validation.
/// </summary>
public static class JwtFactory
{
    public const string TestSecret   = "test-only-secret-min-32-chars-long-ok";
    public const string TestIssuer   = "classmate-api";
    public const string TestAudience = "classmate-frontend";

    public static string CreateToken(int userId, string email = "test@example.com")
    {
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddHours(1);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier,     userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             TestIssuer,
            audience:           TestAudience,
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Convenience builders for test entity objects.
/// </summary>
public static class TestData
{
    public static User MakeUser(
        int    id            = 1,
        string googleId      = "google-sub-12345",
        string email         = "student@example.com",
        string displayName   = "Test Student",
        int    freeUsages    = 5,
        string refreshToken  = "fake-refresh-token")
    {
        return new User
        {
            Id                  = id,
            GoogleId            = googleId,
            Email               = email,
            DisplayName         = displayName,
            AvatarUrl           = "https://example.com/avatar.jpg",
            GoogleRefreshToken  = refreshToken,
            NotificationEmail   = email,
            FreeUsagesRemaining = freeUsages,
            CreatedAt           = DateTime.UtcNow,
            UpdatedAt           = DateTime.UtcNow
        };
    }

    public static ClassroomSetting MakeSetting(
        int    userId       = 1,
        string courseId     = "course-abc",
        string courseName   = "Calculus II",
        bool   autoSolve    = false,
        int    delayMinutes = 0)
    {
        return new ClassroomSetting
        {
            UserId       = userId,
            CourseId     = courseId,
            CourseName   = courseName,
            AutoSolve    = autoSolve,
            DelayMinutes = delayMinutes,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };
    }
}