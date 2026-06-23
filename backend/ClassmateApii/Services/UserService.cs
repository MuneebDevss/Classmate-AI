using Google.Apis.Auth;
using ClassmateApii.Data;
using ClassmateApii.DTOs;
using ClassmateApii.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClassmateApii.Services;

public interface IUserService
{
    Task<AuthResponse> AuthenticateWithGoogleAsync(GoogleAuthRequest req, CancellationToken ct = default);
    Task<UserDto> GetUserDtoAsync(int userId, CancellationToken ct = default);
    Task UpsertClassroomSettingAsync(int userId, UpsertClassroomSettingRequest req, CancellationToken ct = default);
    
    /// <summary>
    /// Roman Urdu: ClassroomRegistrationService ko Google API call karne ke liye
    /// fresh access token chahiye. Yeh method existing token refresh logic use kare.
    /// </summary>
    Task<string> GetFreshAccessTokenAsync(int userId, CancellationToken ct);
}

public partial class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext db, IConfiguration config, ILogger<UserService> logger, IHttpClientFactory httpFactory)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _httpFactory = httpFactory;
    }

    public async Task<AuthResponse> AuthenticateWithGoogleAsync(GoogleAuthRequest req, CancellationToken ct = default)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var clientId = _config["Google:ClientId"] ?? throw new Exception("Google ClientId missing.");
            payload = await GoogleJsonWebSignature.ValidateAsync(req.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Google Token Validation Failed: {Message}", ex.Message);
            throw new UnauthorizedAccessException("Invalid Google ID token.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject, ct);

        if (user is null)
        {
            user = new User
            {
                GoogleId = payload.Subject,
                Email = payload.Email,
                DisplayName = payload.Name,
                AvatarUrl = payload.Picture,
                GoogleRefreshToken = req.RefreshToken,
                NotificationEmail = payload.Email,
                FreeUsagesRemaining = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
        }
        else
        {
            // Update token if a new one is provided during login
            if (!string.IsNullOrEmpty(req.RefreshToken))
                user.GoogleRefreshToken = req.RefreshToken;
            
            user.DisplayName = payload.Name;
            user.AvatarUrl = payload.Picture;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        var token = IssueJwt(user);

        return new AuthResponse(token, MapToDto(user));
    }

    public async Task<UserDto> GetUserDtoAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync(new object[] { userId }, ct)
            ?? throw new NotFoundException("User", userId);
        return MapToDto(user);
    }

    public async Task UpsertClassroomSettingAsync(int userId, UpsertClassroomSettingRequest req, CancellationToken ct = default)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId, ct);
        if (!userExists) throw new NotFoundException("User", userId);

        var setting = await _db.ClassroomSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.CourseId == req.CourseId, ct);

        if (setting is null)
        {
            setting = new ClassroomSetting
            {
                UserId = userId,
                CourseId = req.CourseId,
                CourseName = req.CourseName,
                CreatedAt = DateTime.UtcNow
            };
            _db.ClassroomSettings.Add(setting);
        }

        setting.AutoSolve = req.AutoSolve;
        setting.DelayMinutes = req.DelayMinutes;
        setting.CourseName = req.CourseName;
        setting.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private string IssueJwt(User user)
    {
        var secret = _config["Jwt:Secret"] ?? throw new Exception("Jwt:Secret missing.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        if (!double.TryParse(_config["Jwt:ExpiryHours"] ?? "24", out var hours))
            hours = 24;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    internal static UserDto MapToDto(User u) => new(
        u.Id,
        u.Email,
        u.DisplayName,
        u.AvatarUrl,
        u.FreeUsagesRemaining,
        HasOpenAiKey: !string.IsNullOrEmpty(u.EncryptedOpenAiKey),
        HasGeminiKey: !string.IsNullOrEmpty(u.EncryptedGeminiKey),
        u.NotificationEmail);


}