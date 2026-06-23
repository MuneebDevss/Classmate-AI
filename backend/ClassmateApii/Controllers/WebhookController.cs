using ClassmateApii.Data;
using ClassmateApii.DTOs;
using ClassmateApii.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ClassmateApii.Controllers;

// Roman Urdu: Yeh controller Google Classroom ki push notifications receive karta hai.
// Jab teacher koi assignment publish kare, Google humein yahan POST request bhejta hai.
// Yeh endpoint [AllowAnonymous] hai kyunki Google Bearer token nahi bhejta —
// verification hum apne channelToken se karte hain.

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJobQueueService _jobQueue;        // BullMQ job enqueue karne ke liye
    private readonly IClassroomService _classroomService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        AppDbContext db,
        IJobQueueService jobQueue,
        IClassroomService classroomService,
        ILogger<WebhookController> logger)
    {
        _db = db;
        _jobQueue = jobQueue;
        _classroomService = classroomService;
        _logger = logger;
    }

    // ── Main webhook endpoint ─────────────────────────────────────────────────

    /// <summary>
    /// Roman Urdu: Google Classroom yahan POST karta hai jab naya assignment publish ho.
    ///
    /// Google ke important headers:
    ///   X-Goog-Channel-Token   — hamara secret token, verify karne ke liye
    ///   X-Goog-Channel-ID      — channel ID jo humne registration mein diya tha
    ///   X-Goog-Resource-State  — "exists" (new/updated) ya "sync" (test ping)
    ///   X-Goog-Resource-URI    — affected resource ka URL
    ///
    /// Body: Google Classroom push notifications ka body usually empty hota hai.
    /// Hum sirf headers se kaam chalate hain aur phir Classroom API se details fetch karte hain.
    /// </summary>
    [HttpPost("classroom")]
    public async Task<IActionResult> HandleClassroomPush(CancellationToken ct)
    {
        // ── Step 1: Headers nikalo ────────────────────────────────────────────

        var channelToken = Request.Headers["X-Goog-Channel-Token"].ToString();
        var channelId = Request.Headers["X-Goog-Channel-ID"].ToString();
        var resourceState = Request.Headers["X-Goog-Resource-State"].ToString();
        var resourceUri = Request.Headers["X-Goog-Resource-URI"].ToString();

        _logger.LogInformation(
            "Webhook received. State={State} ChannelId={ChannelId} URI={Uri}",
            resourceState, channelId, resourceUri);

        // ── Step 2: "sync" ping handle karo ──────────────────────────────────
        // Roman Urdu: Jab hum pehli baar register karte hain, Google ek test ping bhejta hai
        // with resourceState = "sync". Isko 200 se acknowledge karo, kuch aur karne ki zaroorat nahi.
        if (resourceState == "sync")
        {
            _logger.LogInformation("Sync ping received for channel {ChannelId}. Acknowledged.", channelId);
            return Ok();
        }

        // ── Step 3: Sirf "exists" events process karo ────────────────────────
        // Roman Urdu: "exists" matlab resource create/update hua hai.
        // Koi aur state ignore karo.
        if (resourceState != "exists")
        {
            _logger.LogInformation(
                "Ignoring webhook with unhandled state: {State}", resourceState);
            return Ok();
        }

        // ── Step 4: Channel token se registration DB mein dhundo ─────────────
        // Roman Urdu: Yahan do kaam hote hain:
        // (a) Verify karo ke yeh request genuinely Google ki taraf se hai
        // (b) Pata karo ke kaunse userId aur courseId ke liye hai
        var registration = await _db.ClassroomRegistrations
            .FirstOrDefaultAsync(r => r.ChannelToken == channelToken, ct);

        if (registration == null)
        {
            // Roman Urdu: Unknown token — ya toh stale channel hai ya fake request.
            _logger.LogWarning(
                "Webhook received with unknown channel token. Possible stale or forged request. Token={Token}",
                channelToken);
            // Roman Urdu: 200 return karo taki Google retry na kare, but kuch process mat karo.
            return Ok();
        }

        var userId = registration.UserId;
        var courseId = registration.CourseId;

        // ── Step 5: courseWorkId resource URI se extract karo ─────────────────
        // Roman Urdu: resourceUri aisi hogi:
        // https://classroom.googleapis.com/v1/courses/{courseId}/courseWork/{courseWorkId}
        var courseWorkId = ExtractCourseWorkId(resourceUri);

        if (string.IsNullOrEmpty(courseWorkId))
        {
            // Roman Urdu: URI mein specific courseWork ID nahi — yeh course-level notification hai.
            // Hum specifically assignment-level events chahte hain, skip karo.
            _logger.LogInformation(
                "Webhook for course {CourseId} has no specific courseWorkId in URI. Skipping.", courseId);
            return Ok();
        }

        _logger.LogInformation(
            "Processing assignment event. UserId={UserId} CourseId={CourseId} AssignmentId={AssignmentId}",
            userId, courseId, courseWorkId);

        // ── Step 6: Idempotency check ─────────────────────────────────────────
        // Roman Urdu: Check karo ke is assignment ke liye job already queue mein hai ya nahi.
        // Requirement 4.4: "A job must not be created twice for the same assignment"
        var alreadyQueued = await _db.AssignmentJobs
            .AnyAsync(j => j.UserId == userId
                        && j.CourseId == courseId
                        && j.AssignmentId == courseWorkId, ct);

        if (alreadyQueued)
        {
            _logger.LogInformation(
                "Job already exists for UserId={UserId} AssignmentId={AssignmentId}. Skipping duplicate.",
                userId, courseWorkId);
            return Ok();
        }

        // ── Step 7: User ki course settings nikalo (delay, autoSolve confirm) ──
        var settings = await _db.ClassroomSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.CourseId == courseId, ct);

        if (settings == null || !settings.AutoSolve)
        {
            // Roman Urdu: User ne auto-solve band kar diya hoga webhook expire hone se pehle.
            _logger.LogInformation(
                "AutoSolve is off for UserId={UserId} CourseId={CourseId}. Not queuing job.",
                userId, courseId);
            return Ok();
        }

        // ── Step 8: Assignment details fetch karo Google se ──────────────────
        // Roman Urdu: Webhook body mein assignment ka content nahi hota.
        // Hum Classroom API se full assignment fetch karte hain.
        ClassmateApii.DTOs.AssignmentDto? assignment;
        try
        {
            assignment = await _classroomService.GetAssignmentByIdAsync(
                userId, courseId, courseWorkId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to fetch assignment {AssignmentId} for user {UserId}.", courseWorkId, userId);
            // Roman Urdu: 500 return karo taki Google retry kare (exponential backoff ke saath).
            return StatusCode(500, "Failed to fetch assignment details.");
        }

        if (assignment == null)
        {
            _logger.LogWarning(
                "Assignment {AssignmentId} not found or not PUBLISHED. Skipping.", courseWorkId);
            return Ok();
        }

        // ── Step 9: Job record DB mein save karo (BullMQ ke saath sync) ──────
        // Roman Urdu: Pehle DB mein record banao, phir BullMQ mein enqueue karo.
        // Isse job track hoti hai aur user cancel bhi kar sakta hai (requirement 4.4).
        var jobRecord = new AssignmentJob
        {
            UserId = userId,
            CourseId = courseId,
            CourseName = registration.CourseName,
            AssignmentId = courseWorkId,
            AssignmentTitle = assignment.Title,
            DelayMinutes = settings.DelayMinutes,
            Status = JobStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow,
            ScheduledFor = DateTimeOffset.UtcNow.AddMinutes(
                settings.DelayMinutes > 0 ? settings.DelayMinutes : 0),
        };

        _db.AssignmentJobs.Add(jobRecord);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _logger.LogInformation(
                "Job insertion failed due to unique constraint. Likely a duplicate webhook for UserId={UserId} AssignmentId={AssignmentId}.",
                userId, courseWorkId);
            return Ok();
        }

        // ── Step 10: BullMQ mein enqueue karo configured delay ke saath ───────
        var delayMs = settings.DelayMinutes > 0
            ? (long)TimeSpan.FromMinutes(settings.DelayMinutes).TotalMilliseconds
            : 0;

        await _jobQueue.EnqueueAssignmentJobAsync(new AssignmentJobPayload
        {
            DbJobId = jobRecord.Id,
            UserId = userId,
            CourseId = courseId,
            CourseName = registration.CourseName,
            AssignmentId = courseWorkId,
            AssignmentTitle = assignment.Title,
            AssignmentDescription = assignment.Description,
            Materials = assignment.Materials,
        }, delayMs, ct);

        _logger.LogInformation(
            "Job enqueued for UserId={UserId} AssignmentId={AssignmentId} with delay {Delay}ms.",
            userId, courseWorkId, delayMs);

        // Roman Urdu: 200 return karo — Google ko batao ke notification receive ho gayi.
        return Ok();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Roman Urdu: Resource URI se courseWorkId extract karo.
    /// URI format: .../courses/{courseId}/courseWork/{courseWorkId}
    /// </summary>
    private static string? ExtractCourseWorkId(string resourceUri)
    {
        if (string.IsNullOrEmpty(resourceUri)) return null;

        // Roman Urdu: URI ko parse karo aur "courseWork" segment ke baad wala part lo.
        var uri = new Uri(resourceUri);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].Equals("courseWork", StringComparison.OrdinalIgnoreCase))
                return segments[i + 1];
        }

        return null;
    }
}