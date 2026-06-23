using ClassmateApii.Models;
using Microsoft.EntityFrameworkCore;

namespace ClassmateApii.Data;

// Roman Urdu: Yeh file database entities aur EF Core mapping define karti hai.

// Roman Urdu: User entity jahan OAuth aur profile data store hota hai.

public class User
{
    public int Id { get; set; }
    public string GoogleId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Encrypted Google OAuth refresh token.
    /// Used to call Classroom/Drive APIs in background jobs when the user is offline.
    /// In production, encrypt this with IDataProtectionProvider before storing.
    /// </summary>
    public string GoogleRefreshToken { get; set; } = default!;

    public int FreeUsagesRemaining { get; set; } = 5;

    /// <summary>AES-encrypted OpenAI key — never stored in plaintext.</summary>
    public string? EncryptedOpenAiKey { get; set; }

    /// <summary>AES-encrypted Gemini key — never stored in plaintext.</summary>
    public string? EncryptedGeminiKey { get; set; }

    /// <summary>Where upload confirmation emails are sent.</summary>
    public string NotificationEmail { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ClassroomSetting> ClassroomSettings { get; set; } = new List<ClassroomSetting>();
}

public class ClassroomSetting
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = default!;

    /// <summary>Roman Urdu: Google Classroom course ID ka unique number.</summary>
    public string CourseId { get; set; } = default!;
    public string CourseName { get; set; } = default!;

    /// <summary>Roman Urdu: Kya is course ki nayi assignments auto-solve hongi.</summary>
    public bool AutoSolve { get; set; } = false;

    /// <summary>
    /// Roman Urdu: Assignment aane ke baad kitni der wait karni hai.
    /// 0 = foran. Negative value ka matlab due date se pehle trigger.
    /// </summary>
    public int DelayMinutes { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// Roman Urdu: DbContext database tables ko code ke sath connect karta hai.

public class AppDbContext : DbContext
{
    // Roman Urdu: Dependency injection ke liye constructor.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<ClassroomSetting> ClassroomSettings => Set<ClassroomSetting>();
    public DbSet<ClassroomRegistration> ClassroomRegistrations => Set<ClassroomRegistration>();
    public DbSet<AssignmentJob> AssignmentJobs => Set<AssignmentJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AssignmentJob>(entity =>
        {
            // Ensures exactly one job per assignment per user to prevent webhook race conditions
            entity.HasIndex(j => new { j.UserId, j.CourseId, j.AssignmentId }).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.GoogleId).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.GoogleId).IsRequired().HasMaxLength(128);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.DisplayName).IsRequired().HasMaxLength(256);
            entity.Property(u => u.AvatarUrl).HasMaxLength(1024);
            entity.Property(u => u.GoogleRefreshToken).IsRequired().HasMaxLength(1024);
            entity.Property(u => u.NotificationEmail).IsRequired().HasMaxLength(256);
            entity.Property(u => u.EncryptedOpenAiKey).HasMaxLength(1024);
            entity.Property(u => u.EncryptedGeminiKey).HasMaxLength(1024);
        });

        modelBuilder.Entity<ClassroomSetting>(entity =>
        {
            entity.HasKey(cs => cs.Id);

            // Roman Urdu: Har user aur course ke combination par sirf ek settings row hogi.
            entity.HasIndex(cs => new { cs.UserId, cs.CourseId }).IsUnique();

            entity.HasOne(cs => cs.User)
                  .WithMany(u => u.ClassroomSettings)
                  .HasForeignKey(cs => cs.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(cs => cs.CourseId).IsRequired().HasMaxLength(128);
            entity.Property(cs => cs.CourseName).IsRequired().HasMaxLength(512);
        });
    }
}