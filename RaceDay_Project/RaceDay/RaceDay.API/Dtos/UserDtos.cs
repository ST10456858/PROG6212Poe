using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos;

public class UpdateUserDto
{
    [Required, StringLength(150)] public string FullName { get; set; } = string.Empty;
    [StringLength(150)] public string? OrganisationName { get; set; }
    [StringLength(20)] public string? ContactNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [StringLength(20)] public string? EmergencyContact { get; set; }
}
public record UserProfileDto(int UserId, string FullName, string Email, string Role,
    string? OrganisationName, string? ContactNumber, DateOnly? DateOfBirth, string? EmergencyContact);
