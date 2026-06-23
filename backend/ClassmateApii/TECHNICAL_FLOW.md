# Event-Driven Architecture Flow - Classroom Notifications

## Complete Architecture Explanation
The system implements a real-time event-driven architecture using Google Classroom Webhooks, BullMQ (via Redis), C#, and EF Core to automatically handle new assignments. Instead of polling every N minutes, the system registers a persistent Webhook with Google. When teachers post assignments, Google sends a POST push notification, which efficiently triggers a background job after verifying user settings.

### 1. Registration Lifecycle (`ClassroomRegistrationService`)
- When a user enables "AutoSolve" for a specific course, `RegisterAsync` runs.
- It authenticates the user using their refreshed Google OAuth access token.
- Calls Google Classroom API (`/v1/registrations`) with `feedType="COURSE_WORK_CHANGES"`.
- A deterministic `channelToken` (Hash of `UserId`, `CourseId`, and an App Secret) is created and sent to Google for subsequent webhook validation.
- The webhook registration gets an `ExpiresAt` timestamp of 7 days into the future.
- Stored safely into `ClassroomRegistrations` DB table.

### 2. Webhook Lifecycle (`WebhookController`)
- **Arrival:** Google fires an HTTPS POST to `/api/webhook/classroom`. 
- **Validation:** 
  - Validates `X-Goog-Channel-Token` against DB records.
  - Confirms `X-Goog-Resource-State` equals `exists` (and acks `sync` heartbeat requests).
- **Idempotency & Duplicate Prevention:** Exists via Database checks and EF Core generic catch against DB Unique Indexes. 
- **Details Fetching:** Webhooks only supply an ID. The Controller queries Google Classroom API strictly to grab assignment meta elements.
- **Enqueue:** Triggers `JobQueueService` for queuing.

### 3. Redis / BullMQ Lifecycle (`JobQueueService`)
- We use a direct raw Redis write emulator for `BullMQ`:
  - Fetch unique Job ID by `StringIncrementAsync` on `bull:{QueueName}:id`.
  - Store metadata using `HashSetAsync` inside `bull:{QueueName}:{jobId}`.
  - Using sorted set mechanism `SortedSetAddAsync` (`bull:{QueueName}:delayed`) or `ListLeftPushAsync` (`bull:{QueueName}:wait`), depending on delay constraints.
  - A Redis PubSub is raised (`bull:{QueueName}:delayed`) to tell the external BullMQ Node worker a job has been pushed.
  - Note: While direct manipulation fulfills basic scenarios, Lua scripting is advisable long-term for complete atomicity.

### 4. Cancellation Lifecycle 
- Provides an endpoint allowing a user to prevent completion before scheduled.
- Endpoint `DELETE /api/classroom/jobs/{jobId}/cancel` zeroes in on the exact queue structure.
- Removes raw key signatures in Redis queue hashes (`ListRemoveAsync`, `SortedSetRemoveAsync`, `KeyDeleteAsync`) to completely orphan and dispose of the task.
- Sets state correctly at EF DB level to `JobStatus.Cancelled`. 

### 5. Renewal Lifecycle (`RegistrationRenewalService`)
- Since Google push channels expire strictly after exactly 7 Days, a passive `IHostedService` operates in the background.
- Scans `ClassroomRegistrations` for anything running within `25` hours of `ExpiresAt`.
- Triggers `CallGoogleDeleteAsync` iteratively to drop stale ties followed automatically by recreated persistent hook logic under a newly injected guid. 

### 6. Failure Recovery & Operational Strategy
- **Restarts:** If the webhook API container halts gracefully or forcefully, Redis + DB persistent hooks ensure logic stays completely in-bounds out-of-box since we lean exclusively on persistent BullMQ structures.
- **Exceptions:** Safe catch thresholds within hooks and loops explicitly prevent breaking loops inside `RegistrationRenewalService`.

### 7. Idempotency Strategy
- Regulated via database index (`UserId, CourseId, AssignmentId`) acting defensively in parallel to logical checking to prevent overlapping asynchronous dual webhooks representing exact duplicate notifications from flooding or corrupting state queues.
