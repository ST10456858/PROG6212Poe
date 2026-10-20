using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Services;

public class AuthService : IAuthService
{
    private readonly RaceDayDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(RaceDayDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken)) return null;

        var user = new User { FullName = request.FullName.Trim(), Email = email, Role = request.Role };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        if (user.Role == "Organiser")
            _db.Organisers.Add(new Organiser { UserId = user.UserId, OrganisationName = request.OrganisationName, ContactNumber = request.ContactNumber });
        else
            _db.Participants.Add(new Participant { UserId = user.UserId });
        await _db.SaveChangesAsync(cancellationToken);

        return CreateResponse(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null) return null;
        var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed) return null;
        return CreateResponse(user);
    }

    private AuthResponseDto CreateResponse(User user)
    {
        var key = _configuration["Jwt:Key"]!;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(4),
            signingCredentials: credentials);
        return new AuthResponseDto(new JwtSecurityTokenHandler().WriteToken(token),
            new UserSummaryDto(user.UserId, user.FullName, user.Email, user.Role));
    }
}
