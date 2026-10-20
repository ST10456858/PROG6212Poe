namespace RaceDay.API.Models;

public class Participant
{
    public int ParticipantId { get; set; }
    public int UserId { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? EmergencyContact { get; set; }
    public User User { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
