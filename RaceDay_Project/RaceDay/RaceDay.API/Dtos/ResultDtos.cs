using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos;

public class ResultWriteDto
{
    public TimeOnly? FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int? PositionOverall { get; set; }
    [Range(1, int.MaxValue)] public int? PositionCategory { get; set; }
    [Required, RegularExpression("^(Finished|DNF|DNS)$")] public string Status { get; set; } = "Finished";
}
public record ResultDto(int ResultId, int EnrolmentId, TimeOnly? FinishTime, int? PositionOverall,
    int? PositionCategory, string Status, int ParticipantId, string ParticipantName, string EventName,
    string CategoryName);
