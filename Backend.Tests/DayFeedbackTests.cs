using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LifeHub.Controllers;
using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests;

public class DayFeedbackTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly ApplicationContext context;
    private readonly Guid userId = Guid.NewGuid();
    private readonly DateOnly date = new(2026, 10, 27);

    public DayFeedbackTests()
    {
        connection.Open();
        context = new ApplicationContext(new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        AddUser(userId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public async Task Save_RoundTripsScoreAndOptionalNote(int score)
    {
        var controller = Controller(userId);
        await controller.Save(date, new() { Score = score });
        context.ChangeTracker.Clear();

        var result = await controller.Get(date);
        Assert.Equal(score, result.Value!.Score);
        Assert.Equal(date, result.Value.Date);
        Assert.Null(result.Value.Note);
    }

    [Fact]
    public async Task Save_UpdatesExistingDayAndCanClearNote()
    {
        var controller = Controller(userId);
        await controller.Save(date, new() { Score = 3, Note = "  A hard day  " });
        Assert.Equal("A hard day", (await controller.Get(date)).Value!.Note);
        await controller.Save(date, new() { Score = 8, Note = "   " });

        var feedback = Assert.Single(await context.DayFeedbacks.ToListAsync());
        Assert.Equal(8, feedback.Score);
        Assert.Null(feedback.Note);
    }

    [Fact]
    public async Task Feedback_IsSeparateForEachUserAndDate()
    {
        var otherUser = Guid.NewGuid();
        AddUser(otherUser);
        var controller = Controller(userId);
        await controller.Save(date, new() { Score = 0, Note = "Private note" });
        Assert.IsType<NoContentResult>((await Controller(otherUser).Get(date)).Result);

        await Controller(otherUser).Save(date, new() { Score = 10, Note = "Other user's note" });
        await controller.Save(date.AddDays(-1), new() { Score = 5 });

        Assert.Equal("Private note", (await controller.Get(date)).Value!.Note);
        Assert.Equal(10, (await Controller(otherUser).Get(date)).Value!.Score);
        Assert.Equal(5, (await controller.Get(date.AddDays(-1))).Value!.Score);
        Assert.Equal(3, await context.DayFeedbacks.CountAsync());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(-1, null)]
    [InlineData(11, null)]
    public void Validation_RejectsMissingOrOutOfRangeScores(int? score, string? note)
    {
        var request = new SaveDayFeedbackRequest { Score = score, Note = note };
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [Fact]
    public void Validation_LimitsNoteLength()
    {
        var request = new SaveDayFeedbackRequest { Score = 5, Note = new string('a', 1001) };
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
    }

    [Fact]
    public async Task Migrations_CreateUsableFeedbackTable()
    {
        using var migrationConnection = new SqliteConnection("Data Source=:memory:");
        await migrationConnection.OpenAsync();
        await using var migrated = new ApplicationContext(new DbContextOptionsBuilder<ApplicationContext>()
            .UseSqlite(migrationConnection).Options);
        await migrated.Database.MigrateAsync();
        Assert.False(migrated.Database.HasPendingModelChanges());
        migrated.Users.Add(new User { Id = userId, Name = "Test", Email = "test@example.test", PasswordHash = "unused" });
        migrated.DayFeedbacks.Add(new() { Id = Guid.NewGuid(), UserId = userId, Date = date, Score = 10 });
        await migrated.SaveChangesAsync();
        Assert.Equal(10, (await migrated.DayFeedbacks.SingleAsync()).Score);
    }

    [Fact]
    public async Task Database_RejectsDuplicateDayForSameUser()
    {
        await Controller(userId).Save(date, new() { Score = 5 });
        context.DayFeedbacks.Add(new() { Id = Guid.NewGuid(), UserId = userId, Date = date, Score = 6 });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_RejectsOutOfRangeScore()
    {
        context.DayFeedbacks.Add(new() { Id = Guid.NewGuid(), UserId = userId, Date = date, Score = 11 });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private DayFeedbackController Controller(Guid id) => new(context)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, id.ToString())
                ], "Test"))
            }
        }
    };

    private void AddUser(Guid id)
    {
        context.Users.Add(new User { Id = id, Name = "Test", Email = $"{id}@example.test", PasswordHash = "unused" });
        context.SaveChanges();
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }
}
