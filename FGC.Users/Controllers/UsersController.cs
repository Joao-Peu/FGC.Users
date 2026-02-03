using Microsoft.AspNetCore.Mvc;
using FGC.Users.Application.Interfaces;
using FGC.Users.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using FGC.Users.API.Middlewares;

namespace FGC.Users.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICorrelationContext _correlation;

    public UsersController(IUserService userService, ICorrelationContext correlation)
    {
        _userService = userService;
        _correlation = correlation;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        try
        {
            var res = await _userService.RegisterAsync(req, _correlation.CorrelationId);
            return CreatedAtAction(nameof(GetMe), null, res);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Title = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            var res = await _userService.LoginAsync(req, _correlation.CorrelationId);
            return Ok(res);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Title = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var uid = User.FindFirst("sub")?.Value ?? User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();
        var res = await _userService.GetMeAsync(uid);
        return Ok(res);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] RegisterRequest req)
    {
        var uid = User.FindFirst("sub")?.Value ?? User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();
        var res = await _userService.UpdateMeAsync(uid, req, _correlation.CorrelationId);
        return Ok(res);
    }
}
