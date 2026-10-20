using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
public class ResultsController(RaceDayDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("api/enrolments/{id:int}/results")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(int id, ResultWriteDto request, CancellationToken ct)
    {
        var enrolment = await db.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event)
            .ThenInclude(e => e.Organiser).Include(e => e.Participant).ThenInclude(p => p.User)
            .SingleOrDefaultAsync(e => e.EnrolmentId == id, ct);
        if (enrolment is null) return NotFound();
        if (enrolment.Category.Event.Organiser.UserId != CurrentUserId) return Forbid();
        if (await db.Results.AnyAsync(r => r.EnrolmentId == id, ct))
            return Conflict(new { message = "A result already exists for this enrolment. Use PUT to update it." });
        var item = new Result { EnrolmentId = id, FinishTime = request.FinishTime,
            PositionOverall = request.PositionOverall, PositionCategory = request.PositionCategory, Status = request.Status };
        db.Results.Add(item);
        await db.SaveChangesAsync(ct);
        return Created($"/api/enrolments/{id}/results", new ResultDto(item.ResultId, item.EnrolmentId,
            item.FinishTime, item.PositionOverall, item.PositionCategory, item.Status, enrolment.ParticipantId,
            enrolment.Participant.User.FullName, enrolment.Category.Event.EventName, enrolment.Category.CategoryName));
    }

    [HttpPut("api/results/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, ResultWriteDto request, CancellationToken ct)
    {
        var item = await db.Results.Include(r => r.Enrolment).ThenInclude(e => e.Category)
            .ThenInclude(c => c.Event).ThenInclude(e => e.Organiser)
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant).ThenInclude(p => p.User)
            .SingleOrDefaultAsync(r => r.ResultId == id, ct);
        if (item is null) return NotFound();
        if (item.Enrolment.Category.Event.Organiser.UserId != CurrentUserId) return Forbid();
        item.FinishTime = request.FinishTime; item.PositionOverall = request.PositionOverall;
        item.PositionCategory = request.PositionCategory; item.Status = request.Status;
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(item));
    }

    [HttpGet("api/events/{eventId:int}/results")]
    [AllowAnonymous]
    public async Task<IActionResult> EventResults(int eventId, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId, ct)) return NotFound();
        var results = await db.Results.Where(r => r.Enrolment.Category.EventId == eventId)
            .OrderBy(r => r.PositionOverall)
            .Select(r => new ResultDto(r.ResultId, r.EnrolmentId, r.FinishTime, r.PositionOverall,
                r.PositionCategory, r.Status, r.Enrolment.ParticipantId, r.Enrolment.Participant.User.FullName,
                r.Enrolment.Category.Event.EventName, r.Enrolment.Category.CategoryName)).ToListAsync(ct);
        return Ok(results);
    }

    private static ResultDto ToDto(Result r) => new(r.ResultId, r.EnrolmentId, r.FinishTime,
        r.PositionOverall, r.PositionCategory, r.Status, r.Enrolment.ParticipantId,
        r.Enrolment.Participant.User.FullName, r.Enrolment.Category.Event.EventName,
        r.Enrolment.Category.CategoryName);
}
