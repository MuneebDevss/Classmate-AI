using System.Security.Claims;
using ClassmateApii.DTOs;
using ClassmateApii.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassmateApii.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClassroomController : ControllerBase
{
    private readonly IClassroomService _classroomService;
    private readonly ClassmateApii.Data.AppDbContext _db;
    private readonly IJobQueueService _jobQueue;

    public ClassroomController(IClassroomService classroomService, ClassmateApii.Data.AppDbContext db, IJobQueueService jobQueue)
    {
        _classroomService = classroomService;
        _db = db;
        _jobQueue = jobQueue;
    }

    // Roman Urdu: Token se user ID nikaal kar Google courses fetch karte hain.
    [HttpGet("courses")]
    public async Task<ActionResult<List<CourseDto>>> GetCourses(CancellationToken ct)
    {
        var userId = GetUserIdFromToken();
        var courses = await _classroomService.GetCoursesAsync(userId, ct);
        return Ok(courses);
    }

    // Roman Urdu: Specific course ke assignments fetch karne ka endpoint.
    [HttpGet("courses/{courseId}/assignments")]
    public async Task<ActionResult<List<AssignmentDto>>> GetAssignments(string courseId, CancellationToken ct)
    {
        var userId = GetUserIdFromToken();
        var assignments = await _classroomService.GetAssignmentsAsync(userId, courseId, ct);
        return Ok(assignments);
    }

    [HttpPut("courses/settings")]
    public async Task<IActionResult> UpdateCourseSettings(
        [FromBody] UpsertClassroomSettingRequest req)
    {
        var userId = GetUserIdFromToken();
        
        // Use req.AutoSolve and req.DelayMinutes
        await _classroomService.UpdateCourseSettings(userId, req.CourseId, req.AutoSolve, req.CourseName, req.DelayMinutes);
        
        return Ok();
    }

    [HttpDelete("jobs/{jobId}/cancel")]
    public async Task<IActionResult> CancelJob(int jobId, CancellationToken ct)
    {
        var userId = GetUserIdFromToken();
        var job = await _db.AssignmentJobs.FindAsync(new object[] { jobId }, ct);
        
        if (job == null || job.UserId != userId)
            return NotFound();

        if (job.Status != JobStatus.Queued)
            return BadRequest("Only queued jobs can be cancelled.");

        if (!string.IsNullOrEmpty(job.BullMqJobId))
        {
            await _jobQueue.CancelJobAsync(job.BullMqJobId, ct);
        }

        job.Status = JobStatus.Cancelled;
        await _db.SaveChangesAsync(ct);

        return Ok();
    }

    private int GetUserIdFromToken()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id");
        if (claim == null) throw new UnauthorizedAccessException("User ID not found in token.");
        return int.Parse(claim.Value);
    }
}