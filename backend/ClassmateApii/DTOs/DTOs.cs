using System.ComponentModel.DataAnnotations;

namespace ClassmateApii.DTOs;

// Roman Urdu: Yeh DTOs API aur frontend ke beech data ka contract hain.

// Roman Urdu: Auth related request/response models.

/// <summary>
/// Sent by Next.js after Google OAuth completes.
/// The frontend gets these values from NextAuth's account callback.
/// </summary>
public record GoogleAuthRequest(
    [Required] string IdToken,       // Google ID token — verified server-side via Google's public keys
    [Required] string AccessToken,   // Short-lived access token (used immediately if needed)
    [Required] string RefreshToken   // Long-lived refresh token (stored for background jobs)
);

public record AuthResponse(
    string Token,   // Our own JWT — frontend stores this and sends as Bearer token
    UserDto User
);

// ── User ──────────────────────────────────────────────────────────────────────

// Roman Urdu: Logged-in user ka lightweight profile model.
public record UserDto(
    int Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    int FreeUsagesRemaining,
    bool HasOpenAiKey,
    bool HasGeminiKey,
    string NotificationEmail
);

// ── Classroom ─────────────────────────────────────────────────────────────────

// Roman Urdu: Google Classroom course ka summary model.
public record CourseDto(
    string CourseId,
    string Name,
    string? Section,
    string? Description,
    string? TeacherName,
    string CourseState,
    string? EnrollmentCode,
    bool AutoSolve,
    int DelayMinutes
);

// Roman Urdu: Assignment detail model jisme due date aur materials bhi hain.
public record AssignmentDto(
    string AssignmentId,
    string CourseId,
    string Title,
    string? Description,
    string? DueDate,        // ISO 8601 UTC string, or null
    double? MaxPoints,
    string WorkType,        // ASSIGNMENT | SHORT_ANSWER_QUESTION | MULTIPLE_CHOICE_QUESTION
    string State,           // PUBLISHED | DRAFT
    List<MaterialDto> Materials
);

// Roman Urdu: Assignment ke attachments ya links ke liye common model.
public record MaterialDto(
    string Type,            // driveFile | link | youtubeVideo | form | unknown
    string Title,
    string? Url,
    string? DriveFileId,
    string? ThumbnailUrl
);

// ── Settings ──────────────────────────────────────────────────────────────────

// Roman Urdu: User ki classroom auto-solve settings update karne ka payload.
public record UpsertClassroomSettingRequest(
    [Required] string CourseId,
    bool AutoSolve,
    int DelayMinutes,
    string CourseName
);

public record AssignmentJobPayload
{
    public int DbJobId { get; init; }
    public int UserId { get; init; }
    public string CourseId { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
    public string AssignmentId { get; init; } = string.Empty;
    public string AssignmentTitle { get; init; } = string.Empty;
    public string? AssignmentDescription { get; init; }
    public List<MaterialDto> Materials { get; init; } = new();
}