using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, RaceDayDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto request, CancellationToken ct)
    {
        if (request.Role == "Organiser" && string.IsNullOrWhiteSpace(request.OrganisationName))
            ModelState.AddModelError(nameof(request.OrganisationName), "Organisation name is required for organisers.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var response = await authService.RegisterAsync(request, ct);
            if (response is null) return Conflict(new { message = "That email address is already registered." });
            return Created("/api/users/me", response);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "That email address is already registered." });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request, CancellationToken ct)
    {
        var response = await authService.LoginAsync(request, ct);
        return response is null ? Unauthorized(new { message = "Invalid email or password." }) : Ok(response);
    }
}
