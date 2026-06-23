
using System.Text.Json;
using ClassmateApii.DTOs;
using StackExchange.Redis;

public interface IJobQueueService
{
    Task EnqueueAssignmentJobAsync(AssignmentJobPayload payload, long delayMs, CancellationToken ct);
    Task CancelJobAsync(string bullMqJobId, CancellationToken ct = default);
}


// ── Implementation ────────────────────────────────────────────────────────────
 
// Roman Urdu: BullMQ Node.js side pe chalti hai, lekin Redis directly hum C# se
// bhi write kar sakte hain — BullMQ ka internal Redis format follow karke.
//
// BullMQ internally Redis mein yeh keys use karta hai:
//   bull:{queueName}:wait       — jobs ready to run (sorted set by score=0)
//   bull:{queueName}:delayed    — future jobs (sorted set, score = unix ms timestamp)
//   bull:{queueName}:{id}       — job data hash
//   bull:{queueName}:id         — auto-increment counter for job IDs
//
// Hum sirf Redis mein yeh structures directly write karte hain.
// BullMQ worker dusri taraf (Node.js ya .NET) inhe automatically pick up karega.
public class JobQueueService : IJobQueueService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<JobQueueService> _logger;
 
    // Roman Urdu: Yeh queue name BullMQ worker side pe bhi same hona chahiye.
    // appsettings.json mein configure karo agar change karna ho.
    private const string QueueName = "assignment-jobs";
 
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
 
    public JobQueueService(
        IConnectionMultiplexer redis,
        ILogger<JobQueueService> logger)
    {
        _redis = redis;
        _logger = logger;
    }
 
    public async Task EnqueueAssignmentJobAsync(
        AssignmentJobPayload payload,
        long delayMs,
        CancellationToken ct)
    {
        var db = _redis.GetDatabase();
 
        // ── Step 1: Unique job ID generate karo ──────────────────────────────
        // Roman Urdu: BullMQ ka counter Redis mein store hota hai.
        // Hum usi counter ko increment karke apna ID lete hain.
        var jobId = await db.StringIncrementAsync($"bull:{QueueName}:id");
 
        // ── Step 2: Job data hash Redis mein store karo ───────────────────────
        // Roman Urdu: BullMQ har job ko ek Hash key mein store karta hai.
        // Fields: name, data, opts, timestamp, delay, attempts, processedOn, finishedOn
        var dataJson = JsonSerializer.Serialize(payload, JsonOpts);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
 
        var optsJson = JsonSerializer.Serialize(new
        {
            delay = delayMs,
            attempts = 3,                   // BullMQ ko 3 baar try karne do failure pe
            backoff = new { type = "exponential", delay = 5000 },
        }, JsonOpts);
 
        var hashKey = $"bull:{QueueName}:{jobId}";
 
        await db.HashSetAsync(hashKey, new HashEntry[]
        {
            new("name",       "solve-assignment"),   // BullMQ worker isse job type identify karta hai
            new("data",       dataJson),
            new("opts",       optsJson),
            new("timestamp",  timestamp.ToString()),
            new("delay",      delayMs.ToString()),
            new("attempts",   "0"),
            new("processedOn",""),
            new("finishedOn", ""),
        });
 
        // ── Step 3: Job ko correct queue mein daalo ───────────────────────────
        if (delayMs <= 0)
        {
            // Roman Urdu: Koi delay nahi — seedha wait list mein daalo.
            // BullMQ "wait" list ek Redis List hai (LPUSH).
            await db.ListLeftPushAsync($"bull:{QueueName}:wait", jobId.ToString());
 
            _logger.LogInformation(
                "Enqueued immediate job {JobId} for assignment {AssignmentId}.",
                jobId, payload.AssignmentId);
        }
        else
        {
            // Roman Urdu: Delay hai — "delayed" sorted set mein daalo.
            // Score = jab job run honi chahiye (unix ms timestamp).
            // BullMQ ka internal timer is score ko check karta hai aur
            // sahi waqt pe job ko "wait" list mein move kar deta hai.
            var runAt = timestamp + delayMs;
 
            await db.SortedSetAddAsync(
                $"bull:{QueueName}:delayed",
                jobId.ToString(),
                runAt);
 
            // Roman Urdu: BullMQ ko notify karo ke naya delayed job aaya hai.
            // Yeh keyspace notification hai — BullMQ isse sun raha hota hai.
            await db.PublishAsync(
                RedisChannel.Literal($"bull:{QueueName}:delayed"),
                jobId.ToString());
 
            _logger.LogInformation(
                "Enqueued delayed job {JobId} for assignment {AssignmentId}. Fires at {RunAt} ({DelayMs}ms).",
                jobId, payload.AssignmentId,
                DateTimeOffset.FromUnixTimeMilliseconds(runAt).ToString("HH:mm:ss UTC"),
                delayMs);
        }
 
        // ── Step 4: DbJobId mein BullMQ job ID save karo ─────────────────────
        // Roman Urdu: Yeh important hai taake user job cancel kar sake dashboard se.
        // Caller (WebhookController) DB record update karega is value se.
        // Hum event raise karte hain instead of direct DB call (separation of concerns).
        OnJobEnqueued?.Invoke(payload.DbJobId, jobId.ToString());
    }
 
    // Roman Urdu: WebhookController ya koi aur subscriber is event se
    // BullMQ job ID DB mein save kar sakta hai.
    public event Action<int, string>? OnJobEnqueued;

    public async Task CancelJobAsync(string bullMqJobId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var hashKey = $"bull:{QueueName}:{bullMqJobId}";
        
        // Remove from the 'delayed' and 'wait' sets/lists
        await db.SortedSetRemoveAsync($"bull:{QueueName}:delayed", bullMqJobId);
        await db.ListRemoveAsync($"bull:{QueueName}:wait", bullMqJobId);
        
        // Delete the job hash completely
        await db.KeyDeleteAsync(hashKey);

        _logger.LogInformation("Cancelled and removed job {JobId} from BullMQ structures.", bullMqJobId);
    }
}