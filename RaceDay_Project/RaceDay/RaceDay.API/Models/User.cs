namespace RaceDay.API.Models;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Participant";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Organiser? Organiser { get; set; }
    public Participant? Participant { get; set; }
}
