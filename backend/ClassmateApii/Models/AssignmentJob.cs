using ClassmateApii.Data;

public enum JobStatus { Queued, Processing, Completed, Failed, Cancelled }
 
public class AssignmentJob
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string CourseId { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string AssignmentId { get; set; } = string.Empty;
    public string AssignmentTitle { get; set; } = string.Empty;
    public int DelayMinutes { get; set; }
    public JobStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ScheduledFor { get; set; }
    public string? BullMqJobId { get; set; }   // BullMQ job ID for cancellation
}