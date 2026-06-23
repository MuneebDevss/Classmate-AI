using System.Security.Claims;
using ClassmateApii.DTOs;
using ClassmateApii.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClassmateApii.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    // Roman Urdu: Current logged-in user ka profile data fetch karta hai.
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetProfile(CancellationToken ct)
    {
        var userId = GetUserIdFromToken();
        var userDto = await _userService.GetUserDtoAsync(userId, ct);
        return Ok(userDto);
    }

    // Roman Urdu: Classroom settings (auto-solve, delay etc) update karne ke liye.
    [HttpPost("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpsertClassroomSettingRequest req, CancellationToken ct)
    {
        var userId = GetUserIdFromToken();
        await _userService.UpsertClassroomSettingAsync(userId, req, ct);
        return NoContent(); // 204 Success
    }

    private int GetUserIdFromToken()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) throw new UnauthorizedAccessException("Token is invalid or user ID is missing.");
        return int.Parse(claim.Value);
    }
}