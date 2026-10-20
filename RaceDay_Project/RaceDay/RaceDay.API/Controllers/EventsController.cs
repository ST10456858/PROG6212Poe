using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(RaceDayDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetEvents([FromQuery] DateOnly? from, [FromQuery] string? location, CancellationToken ct)
    {
        var query = db.Events.Include(e => e.Organiser).ThenInclude(o => o.User)
            .Where(e => e.EventDate >= DateOnly.FromDateTime(DateTime.Today));
        if (from.HasValue) query = query.Where(e => e.EventDate >= from.Value);
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(e => e.Location.Contains(location));
        var events = await query.OrderBy(e => e.EventDate)
            .Select(e => new EventSummaryDto(e.EventId, e.EventName, e.Description, e.EventDate, e.StartTime,
                e.Location, e.RouteMapUrl, e.OrganiserId, e.Organiser.OrganisationName)).ToListAsync(ct);
        return Ok(events);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id, CancellationToken ct)
    {
        var item = await db.Events.Include(e => e.Organiser).ThenInclude(o => o.User)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .SingleOrDefaultAsync(e => e.EventId == id, ct);
        if (item is null) return NotFound();
        var categories = item.Categories.Select(c => new CategoryDto(c.CategoryId, c.EventId, c.CategoryName,
            c.DistanceKm, c.EntryFee, c.MaxParticipants, c.Enrolments.Count(x => x.Status == "Confirmed"))).ToList();
        return Ok(new EventDetailDto(item.EventId, item.EventName, item.Description, item.EventDate, item.StartTime,
            item.Location, item.RouteMapUrl, item.OrganiserId, item.Organiser.OrganisationName, categories));
    }

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateEvent(EventWriteDto request, CancellationToken ct)
    {
        if (request.EventDate < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new { message = "Event date cannot be in the past." });
        var organiserId = await db.Organisers.Where(o => o.UserId == CurrentUserId)
            .Select(o => (int?)o.OrganiserId).SingleOrDefaultAsync(ct);
        if (organiserId is null) return BadRequest(new { message = "Organiser profile not found." });
        var item = new Event { OrganiserId = organiserId.Value, EventName = request.EventName.Trim(),
            Description = request.Description, EventDate = request.EventDate, StartTime = request.StartTime,
            Location = request.Location.Trim(), RouteMapUrl = request.RouteMapUrl };
        db.Events.Add(item);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetEvent), new { id = item.EventId }, new EventSummaryDto(item.EventId,
            item.EventName, item.Description, item.EventDate, item.StartTime, item.Location, item.RouteMapUrl,
            item.OrganiserId, null));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateEvent(int id, EventWriteDto request, CancellationToken ct)
    {
        var item = await db.Events.Include(e => e.Organiser).SingleOrDefaultAsync(e => e.EventId == id, ct);
        if (item is null) return NotFound();
        if (item.Organiser.UserId != CurrentUserId) return Forbid();
        if (request.EventDate < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new { message = "Event date cannot be in the past." });
        item.EventName = request.EventName.Trim(); item.Description = request.Description;
        item.EventDate = request.EventDate; item.StartTime = request.StartTime;
        item.Location = request.Location.Trim(); item.RouteMapUrl = request.RouteMapUrl;
        await db.SaveChangesAsync(ct);
        return Ok(new EventSummaryDto(item.EventId, item.EventName, item.Description, item.EventDate,
            item.StartTime, item.Location, item.RouteMapUrl, item.OrganiserId, null));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken ct)
    {
        var item = await db.Events.Include(e => e.Organiser).SingleOrDefaultAsync(e => e.EventId == id, ct);
        if (item is null) return NotFound();
        if (item.Organiser.UserId != CurrentUserId) return Forbid();
        db.Events.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static EventSummaryDto ToSummary(Event e) => new(e.EventId, e.EventName, e.Description,
        e.EventDate, e.StartTime, e.Location, e.RouteMapUrl, e.OrganiserId, e.Organiser.OrganisationName);
}
