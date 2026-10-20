using RaceDay.API.Dtos;

namespace RaceDay.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto?> LoginAsync(LoginDto request, CancellationToken cancellationToken = default);
}
