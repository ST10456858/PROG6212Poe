using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos;

public class CategoryWriteDto
{
    [Required, StringLength(100)] public string CategoryName { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "999.99")] public decimal DistanceKm { get; set; }
    [Range(typeof(decimal), "0", "999999.99")] public decimal EntryFee { get; set; }
    [Range(1, int.MaxValue)] public int? MaxParticipants { get; set; }
}
public record CategoryDto(int CategoryId, int EventId, string CategoryName, decimal DistanceKm,
    decimal EntryFee, int? MaxParticipants, int ConfirmedEnrolments = 0);
