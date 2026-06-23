using ClassmateApii.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClassmateApii.Data;          // Your DbContext — adjust namespace if different
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClassmateApii.Services;

// Roman Urdu: Yeh interface Google Classroom push notification registrations manage karta hai.
// Jab user kisi course ka auto-solve ON kare, hum Google ko register karte hain ke
// "jab bhi is course mein naya assignment aaye, humein POST kar dena".
public interface IClassroomRegistrationService
{
    /// <summary>
    /// Called when user toggles auto-solve ON for a course.
    /// Registers a push notification channel with Google Classroom for that course.
    /// Stores the registration in DB so it can be renewed before expiry.
    /// </summary>
    Task RegisterAsync(int userId, string courseId, string courseName, CancellationToken ct);

    /// <summary>
    /// Called when user toggles auto-solve OFF for a course.
    /// Deletes the push notification channel from Google so we stop receiving events.
    /// </summary>
    Task UnregisterAsync(int userId, string courseId, CancellationToken ct);

    /// <summary>
    /// Called by the daily renewal background service.
    /// Renews all registrations that expire within the next 24 hours.
    /// Google registrations expire after exactly 7 days — we renew at day 6 to be safe.
    /// </summary>
    Task RenewExpiringRegistrationsAsync(CancellationToken ct);
}

public class ClassroomRegistrationService : IClassroomRegistrationService
{
    // Roman Urdu: Yeh sab dependencies constructor injection se milti hain.
    private readonly AppDbContext _db;
    private readonly IUserService _userService;          // Google access token refresh karne ke liye
    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptions<WebhookOptions> _webhookOptions;
    private readonly ILogger<ClassroomRegistrationService> _logger;

    // Roman Urdu: Google Classroom Feeds API ka base URL.
    private const string ClassroomApiBase = "https://classroom.googleapis.com/v1";

    public ClassroomRegistrationService(
        AppDbContext db,
        IUserService userService,
        IHttpClientFactory httpFactory,
        IOptions<WebhookOptions> webhookOptions,
        ILogger<ClassroomRegistrationService> logger)
    {
        _db = db;
        _userService = userService;
        _httpFactory = httpFactory;
        _webhookOptions = webhookOptions;
        _logger = logger;
    }

    // ── Register ─────────────────────────────────────────────────────────────

    public async Task RegisterAsync(int userId, string courseId, string courseName, CancellationToken ct)
    {
        // Roman Urdu: Pehle check karo ke is course ke liye registration already exists to nahi.
        var existing = await _db.ClassroomRegistrations
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId, ct);

        if (existing != null)
        {
            _logger.LogInformation(
                "Registration already exists for user {UserId} course {CourseId}. Skipping.",
                userId, courseId);
            return;
        }

        // Roman Urdu: User ka Google access token nikalo (refreshed).
        var accessToken = await _userService.GetFreshAccessTokenAsync(userId, ct);

        // Roman Urdu: Ek unique channel ID aur secret token banao.
        // Secret token baad mein webhook request verify karne ke liye use hoga.
        var channelId = Guid.NewGuid().ToString();
        var channelToken = GenerateChannelToken(userId, courseId);

        // Roman Urdu: Google Classroom Registrations API ko call karo.
        var registration = await CallGoogleRegisterAsync(
            accessToken, channelId, channelToken, courseId, ct);

        // Roman Urdu: Registration details DB mein save karo.
        var record = new ClassroomRegistration
        {
            UserId = userId,
            CourseId = courseId,
            CourseName = courseName,
            GoogleRegistrationId = registration.RegistrationId,
            ChannelId = channelId,
            ChannelToken = channelToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.ClassroomRegistrations.Add(record);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Registered webhook for user {UserId} course {CourseId}. Expires {ExpiresAt}.",
            userId, courseId, record.ExpiresAt);
    }

    // ── Unregister ───────────────────────────────────────────────────────────

    public async Task UnregisterAsync(int userId, string courseId, CancellationToken ct)
    {
        var record = await _db.ClassroomRegistrations
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId, ct);

        if (record == null)
        {
            _logger.LogWarning(
                "No registration found to delete for user {UserId} course {CourseId}.",
                userId, courseId);
            return;
        }

        try
        {
            var accessToken = await _userService.GetFreshAccessTokenAsync(userId, ct);
            await CallGoogleDeleteAsync(accessToken, record.GoogleRegistrationId, ct);
        }
        catch (Exception ex)
        {
            // Roman Urdu: Google side pe delete fail ho toh bhi DB se hata do.
            // Worst case: Google ka channel expire ho jayega 7 din mein khud.
            _logger.LogWarning(ex,
                "Failed to delete Google registration {RegId}. Removing from DB anyway.",
                record.GoogleRegistrationId);
        }

        _db.ClassroomRegistrations.Remove(record);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Unregistered webhook for user {UserId} course {CourseId}.",
            userId, courseId);
    }

    // ── Renew expiring ────────────────────────────────────────────────────────

    public async Task RenewExpiringRegistrationsAsync(CancellationToken ct)
    {
        // Roman Urdu: Wo saari registrations nikalo jo agle 25 ghante mein expire honge.
        // (7 din - 6 din = day 6 mein renew karein)
        var threshold = DateTimeOffset.UtcNow.AddHours(25);

        var expiring = await _db.ClassroomRegistrations
            .Include(r => r.User)
            .Where(r => r.ExpiresAt <= threshold)
            .ToListAsync(ct);

        _logger.LogInformation(
            "Renewing {Count} expiring registrations.", expiring.Count);

        foreach (var record in expiring)
        {
            try
            {
                var accessToken = await _userService.GetFreshAccessTokenAsync(record.UserId, ct);

                // Roman Urdu: Purani registration delete karo, nayi banao.
                // Google Classroom API direct renewal support nahi karta —
                // delete + re-create hi standard approach hai.
                await CallGoogleDeleteAsync(accessToken, record.GoogleRegistrationId, ct);

                var newChannelId = Guid.NewGuid().ToString();
                var newRegistration = await CallGoogleRegisterAsync(
                    accessToken, newChannelId, record.ChannelToken, record.CourseId, ct);

                record.GoogleRegistrationId = newRegistration.RegistrationId;
                record.ChannelId = newChannelId;
                record.ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);

                _logger.LogInformation(
                    "Renewed registration for user {UserId} course {CourseId}.",
                    record.UserId, record.CourseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to renew registration for user {UserId} course {CourseId}.",
                    record.UserId, record.CourseId);
                // Roman Urdu: Ek fail ho toh baqi continue karo.
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Google API helpers ────────────────────────────────────────────────────

    private async Task<GoogleRegistrationResponse> CallGoogleRegisterAsync(
        string accessToken,
        string channelId,
        string channelToken,
        string courseId,
        CancellationToken ct)
    {
        // Roman Urdu: Google Classroom Registrations.Create API call.
        // Docs: https://developers.google.com/classroom/reference/rest/v1/registrations/create
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var webhookUrl = _webhookOptions.Value.PublicBaseUrl.TrimEnd('/')
                         + "/api/webhook/classroom";

        var body = new
        {
            feed = new
            {
                feedType = "COURSE_WORK_CHANGES",
                courseWorkChangesInfo = new { courseId }
            },
            destinationUrl = webhookUrl,
            // Roman Urdu: channelToken webhook mein wapas milta hai — verification ke liye.
            token = channelToken
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            $"{ClassroomApiBase}/registrations", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Google registration failed ({response.StatusCode}): {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<GoogleRegistrationResponse>(
            responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result ?? throw new InvalidOperationException(
            "Google returned an empty registration response.");
    }

    private async Task CallGoogleDeleteAsync(
        string accessToken,
        string registrationId,
        CancellationToken ct)
    {
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.DeleteAsync(
            $"{ClassroomApiBase}/registrations/{registrationId}", ct);

        // Roman Urdu: 404 ignore karo — shayad already expire ho gaya ho.
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Google delete failed ({response.StatusCode}): {errorBody}");
        }
    }

    // ── Token helper ──────────────────────────────────────────────────────────

    /// <summary>
    /// Roman Urdu: Ek deterministic secret token banao jo userId + courseId se derive ho.
    /// Yeh webhook receive karte waqt verify karta hai ke request genuine hai.
    /// </summary>
    private string GenerateChannelToken(int userId, string courseId)
    {
        var secret = _webhookOptions.Value.WebhookSecret;
        var raw = $"{userId}:{courseId}:{secret}";
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// ── Supporting models (keep in the same file or move to Models/) ──────────────

// Roman Urdu: Google ki registration create API ka response.
public class GoogleRegistrationResponse
{
    public string RegistrationId { get; set; } = string.Empty;
}

// Roman Urdu: appsettings.json mein yeh section hona chahiye:
// "Webhook": { "PublicBaseUrl": "https://your-app.railway.app", "WebhookSecret": "random-secret" }
public class WebhookOptions
{
    public string PublicBaseUrl { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}