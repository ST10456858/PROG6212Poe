using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Services;
using Xunit;

namespace RaceDay.Tests;

public class AuthControllerTests
{
    private static RaceDayDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new RaceDayDbContext(options);
    }

    [Fact]
    public async Task Register_ReturnsCreated_WhenRegistrationSucceeds()
    {
        var response = new AuthResponseDto("test-token", new UserSummaryDto(1, "Test User", "test@example.com", "Participant"));
        var service = new Mock<IAuthService>();
        service.Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        using var db = CreateDb();
        var controller = new AuthController(service.Object, db);
        var result = await controller.Register(new RegisterDto
        {
            FullName = "Test User", Email = "test@example.com", Password = "Password123", Role = "Participant"
        }, CancellationToken.None);
        Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var service = new Mock<IAuthService>();
        service.Setup(s => s.LoginAsync(It.IsAny<LoginDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthResponseDto?)null);
        using var db = CreateDb();
        var controller = new AuthController(service.Object, db);
        var result = await controller.Login(new LoginDto { Email = "bad@example.com", Password = "wrong" }, CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
