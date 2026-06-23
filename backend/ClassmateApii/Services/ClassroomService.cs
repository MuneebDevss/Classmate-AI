// ─── Type alias MUST be at the very top of the file, before namespace ─────────
// This avoids the naming clash between our ClassroomService class
// and Google's Google.Apis.Classroom.v1.ClassroomService class.
// ─── Type alias MUST be at the very top of the file ──────────────────────────
using GoogleClassroomService = Google.Apis.Classroom.v1.ClassroomService;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using ClassmateApii.Data;
using ClassmateApii.DTOs;
using ClassmateApii.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ClassmateApii.Services;

public interface IClassroomService
{
    Task<List<CourseDto>> GetCoursesAsync(int userId, CancellationToken ct = default);
    Task<List<AssignmentDto>> GetAssignmentsAsync(int userId, string courseId, CancellationToken ct = default);

    Task<ClassroomSetting?> UpdateCourseSettings(int userId, string courseId, bool autoSolve, string courseName, int delayMinutes);

    Task<AssignmentDto?> GetAssignmentByIdAsync(int userId, string courseId, string courseWorkId, CancellationToken ct);
}

public class ClassroomService : IClassroomService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ClassroomService> _logger;

    public ClassroomService(AppDbContext db, IConfiguration config, ILogger<ClassroomService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    private async Task<GoogleClassroomService> BuildClientAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new BadRequestException("User has not connected their Google account (missing refresh token).");

        var clientId = _config["Google:ClientId"] ?? throw new Exception("Google ClientId missing in config.");
        var clientSecret = _config["Google:ClientSecret"] ?? throw new Exception("Google ClientSecret missing in config.");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            Scopes = new[]
            {
                GoogleClassroomService.Scope.ClassroomCoursesReadonly,
                GoogleClassroomService.Scope.ClassroomCourseworkMeReadonly,
                GoogleClassroomService.Scope.ClassroomCourseworkStudentsReadonly,
                DriveService.Scope.DriveReadonly
            }
        });

        var tokenResponse = new TokenResponse { RefreshToken = refreshToken };
        var credential = new UserCredential(flow, "user", tokenResponse);

        // Roman Urdu: Refreshing access token.
        await credential.RefreshTokenAsync(ct);

        return new GoogleClassroomService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "ClassMate AI"
        });
    }

    public async Task<List<CourseDto>> GetCoursesAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.ClassroomSettings)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User", userId);

        using var svc = await BuildClientAsync(user.GoogleRefreshToken, ct);
        var settingsMap = user.ClassroomSettings.ToDictionary(s => s.CourseId);
        var courses = new List<CourseDto>();
        string? nextPage = null;

        do
        {
            var request = svc.Courses.List();
            request.StudentId = "me";
            request.PageSize = 50;
            request.PageToken = nextPage;

            try
            {
                var response = await request.ExecuteAsync(ct);
                foreach (var c in response.Courses ?? Enumerable.Empty<Course>())
                {
                    if (!string.Equals(c.CourseState, "ACTIVE", StringComparison.OrdinalIgnoreCase)) continue;

                    settingsMap.TryGetValue(c.Id, out var setting);
                    courses.Add(new CourseDto(
                        CourseId: c.Id,
                        Name: c.Name,
                        Section: c.Section,
                        Description: c.Description,
                        TeacherName: null, 
                        CourseState: c.CourseState ?? "ACTIVE",
                        EnrollmentCode: c.EnrollmentCode,
                        AutoSolve: setting?.AutoSolve ?? false,
                        DelayMinutes: setting?.DelayMinutes ?? 0));
                }
                nextPage = response.NextPageToken;
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, "Google API error for User {UserId}", userId);
                throw new ExternalServiceException("Google Classroom fetch failed.", ex);
            }
        } while (nextPage is not null);

        return courses;
    }

    public async Task<List<AssignmentDto>> GetAssignmentsAsync(int userId, string courseId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("User", userId);

        using var svc = await BuildClientAsync(user.GoogleRefreshToken, ct);
        var assignments = new List<AssignmentDto>();
        string? nextPage = null;

        do
        {
            var request = svc.Courses.CourseWork.List(courseId);
            request.OrderBy = "updateTime desc";
            request.PageSize = 50;
            request.PageToken = nextPage;

            try
            {
                var response = await request.ExecuteAsync(ct);
                foreach (var cw in response.CourseWork ?? Enumerable.Empty<CourseWork>())
                {
                    if (!string.Equals(cw.State, "PUBLISHED", StringComparison.OrdinalIgnoreCase)) continue;

                    assignments.Add(new AssignmentDto(
                        AssignmentId: cw.Id,
                        CourseId: cw.CourseId,
                        Title: cw.Title,
                        Description: cw.Description,
                        DueDate: FormatDueDate(cw.DueDate, cw.DueTime),
                        MaxPoints: cw.MaxPoints,
                        WorkType: cw.WorkType?.ToString() ?? "ASSIGNMENT",
                        State: cw.State ?? "PUBLISHED",
                        Materials: MapMaterials(cw.Materials)));
                }
                nextPage = response.NextPageToken;
            }
            catch (Google.GoogleApiException ex)
            {
                throw new ExternalServiceException("Failed to fetch assignments.", ex);
            }
        } while (nextPage is not null);

        return assignments;
    }

    private static string? FormatDueDate(Date? date, TimeOfDay? time)
    {
        if (date is null) return null;
        var dt = new DateTime(date.Year ?? 1970, date.Month ?? 1, date.Day ?? 1, 
                              time?.Hours ?? 23, time?.Minutes ?? 59, 0, DateTimeKind.Utc);
        return dt.ToString("o");
    }

    private static List<MaterialDto> MapMaterials(IList<Material>? materials)
    {
        if (materials is null) return new List<MaterialDto>();
        return materials.Select(m => {
            if (m.DriveFile != null) return new MaterialDto("driveFile", m.DriveFile.DriveFile?.Title ?? "File", m.DriveFile.DriveFile?.AlternateLink, m.DriveFile.DriveFile?.Id, m.DriveFile.DriveFile?.ThumbnailUrl);
            if (m.Link != null) return new MaterialDto("link", m.Link.Title ?? m.Link.Url ?? "Link", m.Link.Url, null, m.Link.ThumbnailUrl);
            if (m.YoutubeVideo != null) return new MaterialDto("youtubeVideo", m.YoutubeVideo.Title ?? "Video", m.YoutubeVideo.AlternateLink, null, m.YoutubeVideo.ThumbnailUrl);
            if (m.Form != null) return new MaterialDto("form", m.Form.Title ?? "Form", m.Form.FormUrl, null, m.Form.ThumbnailUrl);
            return new MaterialDto("unknown", "Attachment", null, null, null);
        }).ToList();
    }

    public async Task<ClassroomSetting?> UpdateCourseSettings(int userId, string courseId, bool autoSolve, string courseName, int delayMinutes)
    {
        try
        {
            var setting = _db.ClassroomSettings.FirstOrDefault(s => s.UserId == userId && s.CourseId == courseId);
            if (setting is null)
            {
                setting = new ClassroomSetting
                {
                    UserId = userId,
                    CourseId = courseId,
                    CourseName = courseName,
                    AutoSolve = autoSolve,
                    DelayMinutes = delayMinutes
                };
                _db.ClassroomSettings.Add(setting);
            }
            else
            {
                setting.AutoSolve = autoSolve;
                setting.DelayMinutes = delayMinutes;
                setting.CourseName = courseName;
                _db.ClassroomSettings.Update(setting);
            }
            _db.SaveChanges();
            return setting;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while updating course settings for User {UserId}, Course {CourseId}", userId, courseId);
            throw new Exception("Failed to update course settings.");
        }
    }

    public Task<AssignmentDto?> GetAssignmentByIdAsync(int userId, string courseId, string courseWorkId, CancellationToken ct)
    {
            try
            {
                var user = _db.Users.Find(userId);
                if (user == null) throw new NotFoundException("User", userId);

                using var svc = BuildClientAsync(user.GoogleRefreshToken, ct).Result;
                var request = svc.Courses.CourseWork.Get(courseId, courseWorkId);
                var cw = request.Execute();

                if (cw == null) return Task.FromResult<AssignmentDto?>(null);

                var assignment = new AssignmentDto(
                    AssignmentId: cw.Id,
                    CourseId: cw.CourseId,
                    Title: cw.Title,
                    Description: cw.Description,
                    DueDate: FormatDueDate(cw.DueDate, cw.DueTime),
                    MaxPoints: cw.MaxPoints,
                    WorkType: cw.WorkType?.ToString() ?? "ASSIGNMENT",
                    State: cw.State ?? "PUBLISHED",
                    Materials: MapMaterials(cw.Materials));
                return Task.FromResult<AssignmentDto?>(assignment);
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, "Google API error while fetching assignment {CourseWorkId} for User {UserId}", courseWorkId, userId);
                throw new ExternalServiceException("Failed to fetch assignment details from Google Classroom.", ex);
            }
             catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching assignment {CourseWorkId} for User {UserId}", courseWorkId, userId);
                throw new Exception("An unexpected error occurred while fetching assignment details.");
            }
    
    }
}