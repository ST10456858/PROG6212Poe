using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(RaceDayDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken ct)
    {
        var user = await db.Users.Include(x => x.Organiser).Include(x => x.Participant)
            .SingleOrDefaultAsync(x => x.UserId == CurrentUserId, ct);
        if (user is null) return NotFound();
        return Ok(ToProfile(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateUserDto request, CancellationToken ct)
    {
        var user = await db.Users.Include(x => x.Organiser).Include(x => x.Participant)
            .SingleOrDefaultAsync(x => x.UserId == CurrentUserId, ct);
        if (user is null) return NotFound();
        user.FullName = request.FullName.Trim();
        if (user.Role == "Organiser" && user.Organiser is not null)
        {
            user.Organiser.OrganisationName = request.OrganisationName;
            user.Organiser.ContactNumber = request.ContactNumber;
        }
        if (user.Role == "Participant" && user.Participant is not null)
        {
            user.Participant.DateOfBirth = request.DateOfBirth;
            user.Participant.EmergencyContact = request.EmergencyContact;
        }
        await db.SaveChangesAsync(ct);
        return Ok(ToProfile(user));
    }

    [HttpGet("me/enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> MyEnrolments(CancellationToken ct)
    {
        var participantId = await db.Participants.Where(p => p.UserId == CurrentUserId)
            .Select(p => (int?)p.ParticipantId).SingleOrDefaultAsync(ct);
        if (participantId is null) return NotFound(new { message = "Participant profile not found." });
        var items = await db.Enrolments.Where(e => e.ParticipantId == participantId)
            .Select(e => new EnrolmentDto(e.EnrolmentId, e.ParticipantId, e.Participant.User.FullName,
                e.Participant.User.Email, e.CategoryId, e.Category.CategoryName, e.Category.EventId,
                e.Category.Event.EventName, e.EnrolmentDate, e.Status)).ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("me/results")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> MyResults(CancellationToken ct)
    {
        var participantId = await db.Participants.Where(p => p.UserId == CurrentUserId)
            .Select(p => (int?)p.ParticipantId).SingleOrDefaultAsync(ct);
        if (participantId is null) return NotFound(new { message = "Participant profile not found." });
        var results = await db.Results.Where(r => r.Enrolment.ParticipantId == participantId)
            .Select(r => new ResultDto(r.ResultId, r.EnrolmentId, r.FinishTime, r.PositionOverall,
                r.PositionCategory, r.Status, r.Enrolment.ParticipantId, r.Enrolment.Participant.User.FullName,
                r.Enrolment.Category.Event.EventName, r.Enrolment.Category.CategoryName)).ToListAsync(ct);
        return Ok(results);
    }

    private static UserProfileDto ToProfile(User user) => new(user.UserId, user.FullName, user.Email, user.Role,
        user.Organiser?.OrganisationName, user.Organiser?.ContactNumber,
        user.Participant?.DateOfBirth, user.Participant?.EmergencyContact);
}
