using ClassmateApii.Exceptions;
using System.Net;
using System.Text.Json;

namespace ClassmateApii.Middleware;

// Roman Urdu: Yeh middleware application ke saare unhandled errors pakarta hai.
/// <summary>
/// Catches all unhandled exceptions and converts them to a consistent
/// JSON error response. This means controllers never need try/catch for
/// routing-level errors.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Roman Urdu: Exception type ke hisaab se proper HTTP status code choose karte hain.
        var (statusCode, message) = ex switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized,     ex.Message),
            ForbiddenException          => (HttpStatusCode.Forbidden,        ex.Message),
            NotFoundException           => (HttpStatusCode.NotFound,         ex.Message),
            UsageLimitException         => (HttpStatusCode.PaymentRequired,  ex.Message),
            ExternalServiceException    => (HttpStatusCode.BadGateway,       "An external service error occurred. Please try again shortly."),
            ArgumentException           => (HttpStatusCode.BadRequest,       ex.Message),
            _                           => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        // Roman Urdu: Server-side masla ho to error log, warna warning log karte hain.
        if ((int)statusCode >= 500)
            _logger.LogError(ex, "Unhandled exception: {Type} — {Message}", ex.GetType().Name, ex.Message);
        else
            _logger.LogWarning("Handled exception: {Type} ({Status}) — {Message}",
                ex.GetType().Name, (int)statusCode, ex.Message);

        context.Response.StatusCode  = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new
        {
            error      = message,
            statusCode = (int)statusCode,
            traceId    = context.TraceIdentifier
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(body);
    }
}