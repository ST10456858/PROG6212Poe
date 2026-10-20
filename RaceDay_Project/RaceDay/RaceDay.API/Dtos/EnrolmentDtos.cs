namespace RaceDay.API.Dtos;

public record EnrolmentDto(int EnrolmentId, int ParticipantId, string ParticipantName, string ParticipantEmail,
    int CategoryId, string CategoryName, int EventId, string EventName, DateTime EnrolmentDate, string Status);
