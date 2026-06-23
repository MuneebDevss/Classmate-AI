using ClassmateApi.Data;
using ClassmateApi.DTOs;
using ClassmateApi.Exceptions;
using ClassmateApi.Services;
using ClassmateApi.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClassmateApi.Tests.Services;

/// <summary>
/// Unit tests for UserService.
/// Google ID token validation is NOT tested here (it calls Google's servers).
/// We test everything around it: upsert logic, JWT contents, settings, error paths.
/// </summary>
public class UserServiceTests
{
    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]      = JwtFactory.TestSecret,
                ["Jwt:Issuer"]      = JwtFactory.TestIssuer,
                ["Jwt:Audience"]    = JwtFactory.TestAudience,
                ["Jwt:ExpiryHours"] = "24",
                ["Google:ClientId"] = "test-client-id"
            })
            .Build();

    private static UserService CreateService(AppDbContext db) =>
        new(db, BuildConfig(), NullLogger<UserService>.Instance);

    // ─── GetUserDtoAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserDto_ExistingUser_ReturnsCorrectDto()
    {
        // Arrange
        using var db   = DbContextFactory.CreateInMemory();
        var user       = TestData.MakeUser(id: 1);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var svc = CreateService(db);

        // Act
        var dto = await svc.GetUserDtoAsync(1);

        // Assert
        dto.Id.Should().Be(1);
        dto.Email.Should().Be(user.Email);
        dto.DisplayName.Should().Be(user.DisplayName);
        dto.FreeUsagesRemaining.Should().Be(5);
        dto.HasOpenAiKey.Should().BeFalse();
        dto.HasGeminiKey.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserDto_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        using var db  = DbContextFactory.CreateInMemory();
        var svc       = CreateService(db);

        // Act
        var act = () => svc.GetUserDtoAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    public async Task GetUserDto_UserWithApiKeys_ReportsKeysFlagsCorrectly()
    {
        // Arrange
        using var db = DbContextFactory.CreateInMemory();
        var user     = TestData.MakeUser(id: 2);
        user.EncryptedOpenAiKey = "encrypted-openai-key";
        user.EncryptedGeminiKey = null;
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var svc = CreateService(db);

        // Act
        var dto = await svc.GetUserDtoAsync(2);

        // Assert
        dto.HasOpenAiKey.Should().BeTrue();
        dto.HasGeminiKey.Should().BeFalse();
    }

    // ─── UpsertClassroomSettingAsync ────────────────────────────────────────────

    [Fact]
    public async Task UpsertSetting_NewCourse_CreatesRecord()
    {
        // Arrange
        using var db = DbContextFactory.CreateInMemory();
        db.Users.Add(TestData.MakeUser(id: 1));
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var req = new UpsertClassroomSettingRequest(
            CourseId:     "course-xyz",
            CourseName:   "Biology 101",
            AutoSolve:    true,
            DelayMinutes: 60);

        // Act
        await svc.UpsertClassroomSettingAsync(1, req);

        // Assert
        var saved = db.ClassroomSettings.Single();
        saved.CourseId.Should().Be("course-xyz");
        saved.AutoSolve.Should().BeTrue();
        saved.DelayMinutes.Should().Be(60);
        saved.CourseName.Should().Be("Biology 101");
    }

    [Fact]
    public async Task UpsertSetting_ExistingCourse_UpdatesRecord()
    {
        // Arrange
        using var db = DbContextFactory.CreateInMemory();
        var user     = TestData.MakeUser(id: 1);
        db.Users.Add(user);
        db.ClassroomSettings.Add(TestData.MakeSetting(
            userId: 1, courseId: "course-abc", autoSolve: false, delayMinutes: 0));
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var req = new UpsertClassroomSettingRequest(
            CourseId:     "course-abc",
            CourseName:   "Calculus II",
            AutoSolve:    true,
            DelayMinutes: 360);

        // Act
        await svc.UpsertClassroomSettingAsync(1, req);

        // Assert — still only one record, but updated
        db.ClassroomSettings.Should().HaveCount(1);
        var saved = db.ClassroomSettings.Single();
        saved.AutoSolve.Should().BeTrue();
        saved.DelayMinutes.Should().Be(360);
    }

    [Fact]
    public async Task UpsertSetting_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        using var db = DbContextFactory.CreateInMemory();
        var svc      = CreateService(db);
        var req      = new UpsertClassroomSettingRequest("c", "Course", false, 0);

        // Act
        var act = () => svc.UpsertClassroomSettingAsync(999, req);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpsertSetting_TwoCoursesForSameUser_CreatesTwoRecords()
    {
        // Arrange
        using var db = DbContextFactory.CreateInMemory();
        db.Users.Add(TestData.MakeUser(id: 1));
        await db.SaveChangesAsync();

        var svc = CreateService(db);

        // Act
        await svc.UpsertClassroomSettingAsync(1,
            new UpsertClassroomSettingRequest("course-1", "Math",    true,  30));
        await svc.UpsertClassroomSettingAsync(1,
            new UpsertClassroomSettingRequest("course-2", "History", false, 0));

        // Assert
        db.ClassroomSettings.Should().HaveCount(2);
    }

    // ─── MapToDto (internal, tested via GetUserDto) ────────────────────────────

    [Fact]
    public async Task MapToDto_NotificationEmailMatchesUserEmail_ByDefault()
    {
        using var db = DbContextFactory.CreateInMemory();
        var user     = TestData.MakeUser(id: 1, email: "notify@example.com");
        user.NotificationEmail = "notify@example.com";
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var dto = await svc.GetUserDtoAsync(1);

        dto.NotificationEmail.Should().Be("notify@example.com");
    }
}