using ClassmateApii.Services;

namespace ClassmateApii.BackgroundServices;

// Roman Urdu: Yeh background service roz ek baar chalti hai aur expiring webhook
// registrations ko renew karti hai. Google Classroom registrations sirf 7 din
// ke liye valid hoti hain, isliye hum day 6 pe renew karte hain.
//
// Yeh poori polling cron ki jagah nahi leti — yeh sirf registration renewal ke
// liye hai. Actual assignment detection ab event-driven hai (WebhookController).
//
// IHostedService: .NET ka built-in background service interface.
// AddHostedService<T>() se Program.cs mein register karo.
public class RegistrationRenewalService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegistrationRenewalService> _logger;

    // Roman Urdu: Kitni baar check karna hai — 24 ghante mein ek baar kaafi hai.
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public RegistrationRenewalService(
        IServiceScopeFactory scopeFactory,
        ILogger<RegistrationRenewalService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RegistrationRenewalService started.");

        // Roman Urdu: Server start hone pe pehle thoda wait karo taake app fully ready ho.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunRenewalAsync(stoppingToken);

            // Roman Urdu: Agle 24 ghante tak so jao.
            _logger.LogInformation(
                "RegistrationRenewalService sleeping for {Hours}h. Next run at {NextRun:HH:mm} UTC.",
                Interval.TotalHours,
                DateTime.UtcNow.Add(Interval));

            await Task.Delay(Interval, stoppingToken);
        }

        _logger.LogInformation("RegistrationRenewalService stopping.");
    }

    private async Task RunRenewalAsync(CancellationToken ct)
    {
        // Roman Urdu: Scoped services (DbContext, etc.) ke liye naya scope banao.
        // BackgroundService singleton hoti hai, isliye directly inject nahi kar sakte.
        using var scope = _scopeFactory.CreateScope();
        var registrationService = scope
            .ServiceProvider
            .GetRequiredService<IClassroomRegistrationService>();

        try
        {
            _logger.LogInformation(
                "RegistrationRenewalService running renewal check at {Time:HH:mm} UTC.",
                DateTime.UtcNow);

            await registrationService.RenewExpiringRegistrationsAsync(ct);

            _logger.LogInformation("RegistrationRenewalService renewal check complete.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Roman Urdu: Koi bhi error hone pe service band nahi honi chahiye.
            // Log karo aur agle cycle ka wait karo.
            _logger.LogError(ex, "RegistrationRenewalService encountered an error during renewal.");
        }
    }
}