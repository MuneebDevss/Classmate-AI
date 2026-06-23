using ClassmateApii.DTOs;
using ClassmateApii.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassmateApii.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    // Roman Urdu: Frontend se Google IdToken aur RefreshToken receive karke login process karta hai.
    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleAuthRequest req, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(req.IdToken))
            return BadRequest("Google IdToken is required.");

        var response = await _userService.AuthenticateWithGoogleAsync(req, ct);
        return Ok(response);
    }
}