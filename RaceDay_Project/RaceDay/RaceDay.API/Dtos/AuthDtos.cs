using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos;

public class RegisterDto
{
    [Required, StringLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8), StringLength(100)] public string Password { get; set; } = string.Empty;
    [Required, RegularExpression("^(Organiser|Participant)$")] public string Role { get; set; } = "Participant";
    [StringLength(150)] public string? OrganisationName { get; set; }
    [StringLength(20)] public string? ContactNumber { get; set; }
}
public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}
public record UserSummaryDto(int UserId, string FullName, string Email, string Role);
public record AuthResponseDto(string Token, UserSummaryDto User);
