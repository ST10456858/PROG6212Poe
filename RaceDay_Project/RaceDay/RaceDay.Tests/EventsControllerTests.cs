using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.Tests;

public class EventsControllerTests
{
    private static RaceDayDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new RaceDayDbContext(options);
    }

    [Fact]
    public async Task GetEvent_ReturnsNotFound_WhenEventDoesNotExist()
    {
        using var db = CreateDb();
        var controller = new EventsController(db);
        var result = await controller.GetEvent(999, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetEvents_ReturnsUpcomingEvents()
    {
        using var db = CreateDb();
        var user = new User { UserId = 1, FullName = "Test Organiser", Email = "organiser@example.com",
            PasswordHash = "not-used", Role = "Organiser" };
        var organiser = new Organiser { OrganiserId = 1, UserId = 1, OrganisationName = "Test Club", User = user };
        var futureEvent = new Event { EventId = 1, OrganiserId = 1, Organiser = organiser,
            EventName = "Future Run", EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)), Location = "Johannesburg" };
        var pastEvent = new Event { EventId = 2, OrganiserId = 1, Organiser = organiser,
            EventName = "Past Run", EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)), Location = "Pretoria" };
        db.Users.Add(user);
        db.Organisers.Add(organiser);
        db.Events.AddRange(futureEvent, pastEvent);
        await db.SaveChangesAsync();

        var controller = new EventsController(db);
        var result = await controller.GetEvents(null, null, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        Assert.Single(events);
    }
}
