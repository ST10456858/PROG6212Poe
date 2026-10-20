using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos;

public class EventWriteDto
{
    [Required, StringLength(150)] public string EventName { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    [Required, StringLength(200)] public string Location { get; set; } = string.Empty;
    [Url, StringLength(255)] public string? RouteMapUrl { get; set; }
}
public record EventSummaryDto(int EventId, string EventName, string? Description, DateOnly EventDate,
    TimeOnly? StartTime, string Location, string? RouteMapUrl, int OrganiserId, string? OrganisationName);
public record EventDetailDto(int EventId, string EventName, string? Description, DateOnly EventDate,
    TimeOnly? StartTime, string Location, string? RouteMapUrl, int OrganiserId, string? OrganisationName,
    IReadOnlyList<CategoryDto> Categories);
