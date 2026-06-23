namespace ClassmateApii.Exceptions;

// Roman Urdu: Yeh custom exceptions middleware ko semantic HTTP responses dene mein madad karti hain.

/// <summary>
/// Thrown when an external service (Google APIs, etc.) returns an error.
/// Maps to HTTP 502 Bad Gateway in the error handling middleware.
/// </summary>
public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message) : base(message) { }
    public ExternalServiceException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Thrown when a user attempts an action they are not permitted to perform.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a requested resource does not exist.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string resource, object id)
        : base($"{resource} with id '{id}' was not found.") { }
}
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}
/// <summary>
/// Thrown when a user has exhausted their free AI usage tier
/// and has not provided their own API key.
/// Maps to HTTP 402 Payment Required.
/// </summary>
public class UsageLimitException : Exception
{
    public UsageLimitException(string message) : base(message) { }
}