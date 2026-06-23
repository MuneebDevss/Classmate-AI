using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassmateApii.Data;
using Microsoft.EntityFrameworkCore;

namespace ClassmateApii.Services;

// Roman Urdu: Yeh file sirf GetFreshAccessTokenAsync ka implementation hai.
// Isko apni existing UserService class mein paste karo.
//
// Kya karta hai:
//   1. DB se user ka stored refresh token nikalta hai
//   2. Google OAuth token endpoint ko call karta hai
//   3. Naya access token return karta hai
//   4. Agar refresh token bhi rotate hua ho (Google kabhi kabhi karta hai)
//      toh DB mein update kar deta hai
//   5. Agar refresh token revoked/expired ho toh clear user-friendly error throw karta hai

public partial class UserService : IUserService
{
    // Roman Urdu: Yeh fields tumhari existing UserService mein already honge.
    // Sirf GetFreshAccessTokenAsync method add karo apni class mein.
    // (partial class isliye hai taake existing file touch na karni pade)
    
    private readonly IHttpClientFactory _httpFactory;
    

    // ── GetFreshAccessTokenAsync ──────────────────────────────────────────────

    public async Task<string> GetFreshAccessTokenAsync(int userId, CancellationToken ct)
    {
        // ── Step 1: DB se user nikalo ─────────────────────────────────────────
        // Roman Urdu: User entity mein GoogleRefreshToken column hona chahiye.
        // AuthenticateWithGoogleAsync mein yeh already store ho raha hoga.
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidOperationException(
                $"User {userId} not found in database.");

        if (string.IsNullOrEmpty(user.GoogleRefreshToken))
        {
            // Roman Urdu: Refresh token nahi hai — user ko dobara login karna hoga.
            throw new InvalidOperationException(
                $"No Google refresh token stored for user {userId}. " +
                "User must re-authenticate.");
        }

        // ── Step 2: Google token endpoint call karo ───────────────────────────
        // Roman Urdu: Google ka standard OAuth2 token refresh endpoint.
        // Docs: https://developers.google.com/identity/protocols/oauth2/web-server#offline
        var clientId = _config["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId not configured.");
        var clientSecret = _config["Google:ClientSecret"]
            ?? throw new InvalidOperationException("Google:ClientSecret not configured.");

        var client = _httpFactory.CreateClient();

        var formData = new Dictionary<string, string>
        {
            ["grant_type"]    = "refresh_token",
            ["refresh_token"] = user.GoogleRefreshToken,
            ["client_id"]     = clientId,
            ["client_secret"] = clientSecret,
        };

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(formData),
                ct);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Network error calling Google token endpoint for user {userId}.", ex);
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        // ── Step 3: Error handling ─────────────────────────────────────────────
        if (!response.IsSuccessStatusCode)
        {
            // Roman Urdu: Google "invalid_grant" return karta hai jab:
            //   - Refresh token revoke ho gaya ho (user ne app disconnect kiya)
            //   - Token 6 mahine se use nahi hua
            //   - User ka Google account suspend ho gaya
            // In sab cases mein stored token delete karo aur user ko re-auth karo.
            var errorResponse = TryDeserialize<GoogleTokenErrorResponse>(responseBody);

            if (errorResponse?.Error == "invalid_grant")
            {
                _logger.LogWarning(
                    "Refresh token revoked or expired for user {UserId}. Clearing token.",
                    userId);

                // Roman Urdu: Invalid token DB se hata do — warna har baar fail hota rahega.
                await ClearRefreshTokenAsync(userId, ct);

                throw new GoogleTokenRevokedException(
                    $"Google access has been revoked for user {userId}. " +
                    "User must sign in again.");
            }

            _logger.LogError(
                "Google token refresh failed for user {UserId}. Status={Status} Body={Body}",
                userId, response.StatusCode, responseBody);

            throw new InvalidOperationException(
                $"Google token refresh failed ({response.StatusCode}): {responseBody}");
        }

        // ── Step 4: Response parse karo ───────────────────────────────────────
        var tokenResponse = TryDeserialize<GoogleTokenSuccessResponse>(responseBody)
            ?? throw new InvalidOperationException(
                $"Could not parse Google token response for user {userId}.");

        if (string.IsNullOrEmpty(tokenResponse.AccessToken))
            throw new InvalidOperationException(
                $"Google returned empty access_token for user {userId}.");

        // ── Step 5: Agar Google ne naya refresh token diya toh save karo ──────
        // Roman Urdu: Google kabhi kabhi token rotation karta hai — naya refresh token
        // response mein aata hai. Isko save karna zaroori hai, warna agli baar fail hoga.
        if (!string.IsNullOrEmpty(tokenResponse.RefreshToken)
            && tokenResponse.RefreshToken != user.GoogleRefreshToken)
        {
            _logger.LogInformation(
                "Google rotated refresh token for user {UserId}. Saving new token.", userId);

            await UpdateRefreshTokenAsync(userId, tokenResponse.RefreshToken, ct);
        }

        _logger.LogDebug(
            "Successfully refreshed access token for user {UserId}. Expires in {ExpiresIn}s.",
            userId, tokenResponse.ExpiresIn);

        return tokenResponse.AccessToken;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task ClearRefreshTokenAsync(int userId, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) return;
        user.GoogleRefreshToken = null;
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpdateRefreshTokenAsync(
        int userId, string newToken, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) return;
        user.GoogleRefreshToken = newToken;
        await _db.SaveChangesAsync(ct);
    }

    private static T? TryDeserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return default;
        }
    }
}

// ── Response models ───────────────────────────────────────────────────────────

// Roman Urdu: Google token endpoint ka success response.
internal class GoogleTokenSuccessResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }      // seconds — usually 3600

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    // Roman Urdu: Sirf tab present hoga jab Google token rotate kare.
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

// Roman Urdu: Google token endpoint ka error response.
internal class GoogleTokenErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}

// ── Custom exception ──────────────────────────────────────────────────────────

// Roman Urdu: Yeh specific exception tab throw hoti hai jab user ka Google access
// revoke ho jaye. WebhookController ya JobProcessor isse catch karke user ko
// email notification bhej sakta hai: "Please sign in again to continue."
public class GoogleTokenRevokedException : Exception
{
    public GoogleTokenRevokedException(string message) : base(message) { }
}