using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
public class EnrolmentsController(RaceDayDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("api/categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enrol(int categoryId, CancellationToken ct)
    {
        var participantId = await db.Participants.Where(p => p.UserId == CurrentUserId)
            .Select(p => (int?)p.ParticipantId).SingleOrDefaultAsync(ct);
        if (participantId is null) return BadRequest(new { message = "Participant profile not found." });
        var category = await db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == categoryId, ct);
        if (category is null) return NotFound(new { message = "Category not found." });
        if (category.Event.EventDate < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new { message = "You cannot enrol in a past event." });
        var existing = await db.Enrolments.SingleOrDefaultAsync(e => e.ParticipantId == participantId && e.CategoryId == categoryId, ct);
        if (existing is not null)
        {
            if (existing.Status == "Confirmed") return Conflict(new { message = "You are already enrolled in this category." });
            existing.Status = "Confirmed";
            existing.EnrolmentDate = DateTime.Now;
            await db.SaveChangesAsync(ct);
            return Created($"/api/users/me/enrolments", new { existing.EnrolmentId, existing.ParticipantId, existing.CategoryId, existing.Status });
        }
        if (category.MaxParticipants.HasValue)
        {
            var count = await db.Enrolments.CountAsync(e => e.CategoryId == categoryId && e.Status == "Confirmed", ct);
            if (count >= category.MaxParticipants.Value) return BadRequest(new { message = "This category is full." });
        }
        var enrolment = new Enrolment { ParticipantId = participantId.Value, CategoryId = categoryId, Status = "Confirmed" };
        db.Enrolments.Add(enrolment);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict(new { message = "An enrolment already exists for this participant and category." }); }
        return Created("/api/users/me/enrolments", new { enrolment.EnrolmentId, enrolment.ParticipantId, enrolment.CategoryId, enrolment.EnrolmentDate, enrolment.Status });
    }

    [HttpGet("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> EventEnrolments(int eventId, CancellationToken ct)
    {
        var eventItem = await db.Events.Include(e => e.Organiser).SingleOrDefaultAsync(e => e.EventId == eventId, ct);
        if (eventItem is null) return NotFound();
        if (eventItem.Organiser.UserId != CurrentUserId) return Forbid();
        var items = await db.Enrolments.Where(e => e.Category.EventId == eventId)
            .Select(e => new EnrolmentDto(e.EnrolmentId, e.ParticipantId, e.Participant.User.FullName,
                e.Participant.User.Email, e.CategoryId, e.Category.CategoryName, e.Category.EventId,
                e.Category.Event.EventName, e.EnrolmentDate, e.Status)).ToListAsync(ct);
        return Ok(items);
    }

    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var item = await db.Enrolments.Include(e => e.Participant)
            .SingleOrDefaultAsync(e => e.EnrolmentId == id, ct);
        if (item is null) return NotFound();
        if (item.Participant.UserId != CurrentUserId) return Forbid();
        if (item.Status == "Cancelled") return NoContent();
        item.Status = "Cancelled";
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
