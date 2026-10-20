using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
public class CategoriesController(RaceDayDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("api/events/{eventId:int}/categories")]
    public async Task<IActionResult> GetForEvent(int eventId, CancellationToken ct)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId, ct)) return NotFound();
        var categories = await db.Categories.Where(c => c.EventId == eventId).Include(c => c.Enrolments)
            .Select(c => new CategoryDto(c.CategoryId, c.EventId, c.CategoryName, c.DistanceKm, c.EntryFee,
                c.MaxParticipants, c.Enrolments.Count(x => x.Status == "Confirmed"))).ToListAsync(ct);
        return Ok(categories);
    }

    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(int eventId, CategoryWriteDto request, CancellationToken ct)
    {
        var eventItem = await db.Events.Include(e => e.Organiser).SingleOrDefaultAsync(e => e.EventId == eventId, ct);
        if (eventItem is null) return NotFound();
        if (eventItem.Organiser.UserId != CurrentUserId) return Forbid();
        var item = new Category { EventId = eventId, CategoryName = request.CategoryName.Trim(),
            DistanceKm = request.DistanceKm, EntryFee = request.EntryFee, MaxParticipants = request.MaxParticipants };
        db.Categories.Add(item);
        await db.SaveChangesAsync(ct);
        return Created($"/api/events/{eventId}/categories", new CategoryDto(item.CategoryId, item.EventId,
            item.CategoryName, item.DistanceKm, item.EntryFee, item.MaxParticipants));
    }

    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, CategoryWriteDto request, CancellationToken ct)
    {
        var item = await db.Categories.Include(c => c.Event).ThenInclude(e => e.Organiser)
            .SingleOrDefaultAsync(c => c.CategoryId == id, ct);
        if (item is null) return NotFound();
        if (item.Event.Organiser.UserId != CurrentUserId) return Forbid();
        item.CategoryName = request.CategoryName.Trim(); item.DistanceKm = request.DistanceKm;
        item.EntryFee = request.EntryFee; item.MaxParticipants = request.MaxParticipants;
        await db.SaveChangesAsync(ct);
        return Ok(new CategoryDto(item.CategoryId, item.EventId, item.CategoryName, item.DistanceKm, item.EntryFee, item.MaxParticipants));
    }

    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var item = await db.Categories.Include(c => c.Event).ThenInclude(e => e.Organiser)
            .SingleOrDefaultAsync(c => c.CategoryId == id, ct);
        if (item is null) return NotFound();
        if (item.Event.Organiser.UserId != CurrentUserId) return Forbid();
        db.Categories.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
