using System.Security.Claims;
using LifeHub.Controllers;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Backend.Tests;

public class HabitsTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly ApplicationContext db;
    private readonly Guid userId = Guid.NewGuid();
    private readonly DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static DateOnly Day(string value) => DateOnly.Parse(value);

    public HabitsTests()
    {
        connection.Open();
        db = new(new DbContextOptionsBuilder<ApplicationContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.Users.Add(new() { Id = userId, Name = "Test", Email = "habits@example.test", PasswordHash = "unused" });
        db.SaveChanges();
    }

    private HabitsController Controller(Guid? owner = null) => new(db) { ControllerContext = new() {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, (owner ?? userId).ToString())], "Test")) }
    }};
    private Habit AddHabit(string rule = "FREQ=DAILY", int? goal = null, DateOnly? start = null)
    {
        var h = new Habit { Id = Guid.NewGuid(), UserId = userId, Title = "Read", Color = "#52b58b",
            RecurrenceRule = rule, TimesPerWeekGoal = goal, StartDate = start ?? today.AddDays(-100) };
        h.Schedules.Add(new() { Id = Guid.NewGuid(), EffectiveFrom = h.StartDate, RecurrenceRule = rule, TimesPerWeekGoal = goal });
        db.Habits.Add(h); db.SaveChanges(); return h;
    }
    private static List<HabitDay> Marks(params string[] days) => days.Select(d => new HabitDay { Date = Day(d), Status = HabitDayStatus.Full }).ToList();

    [Fact]
    public void DailyStreak_PreservesYesterdayUntilTodayEnds_AndIgnoresOffDaysAndSkips()
    {
        var h = AddHabit("FREQ=WEEKLY;BYDAY=MO,WE,FR", start: Day("2026-11-02"));
        var history = Marks("2026-11-02", "2026-11-04", "2026-11-09");
        history.Add(new() { Date = Day("2026-11-06"), Status = HabitDayStatus.Skip });
        Assert.Equal(3, HabitProgress.Streak(h, history, Day("2026-11-11")));
        Assert.Equal(0, HabitProgress.Streak(h, history, Day("2026-11-12")));
    }

    [Fact]
    public void DailyPause_IsNeutralWithoutCreatingMarks()
    {
        var h = AddHabit(start: Day("2026-11-02"));
        h.InactivePeriods.Add(new() { StartDate = Day("2026-11-05"), EndDate = Day("2026-11-11") });
        var history = Marks("2026-11-02", "2026-11-03", "2026-11-04");
        Assert.Equal(3, HabitProgress.Streak(h, history, Day("2026-11-11")));
        Assert.False(HabitProgress.IsScheduled(h, Day("2026-11-10")));
        Assert.Equal(3, history.Count);
    }

    [Fact]
    public void WeeklyStreak_CountsWeeks_AndSkipsWholePausedWeeks()
    {
        var h = AddHabit("FREQ=WEEKLY", 3, Day("2026-10-19"));
        h.InactivePeriods.Add(new() { StartDate = Day("2026-10-26"), EndDate = Day("2026-11-02") });
        var history = Marks("2026-10-20", "2026-10-22", "2026-10-24", "2026-11-03", "2026-11-05", "2026-11-07");
        Assert.Equal(2, HabitProgress.Streak(h, history, Day("2026-11-11")));
        Assert.Equal(0, HabitProgress.Streak(h, history, Day("2026-11-16")));
    }

    [Fact]
    public void WeeklyPause_DoesNotReduceGoal_OrBreakAnUnfinishedPartialWeek()
    {
        var h = AddHabit("FREQ=WEEKLY", 3, Day("2026-10-26"));
        h.InactivePeriods.Add(new() { StartDate = Day("2026-11-02"), EndDate = Day("2026-11-05") });
        var history = Marks("2026-10-26", "2026-10-28", "2026-10-30", "2026-11-06", "2026-11-07");
        Assert.Equal(1, HabitProgress.Streak(h, history, Day("2026-11-11")));
        Assert.Equal(3, HabitProgress.ScheduleOn(h, Day("2026-11-07")).Goal);
    }

    [Fact]
    public async Task UpdatingSchedule_IsProspective_AndReplacingPendingChangeKeepsHistory()
    {
        var h = AddHabit();
        db.HabitDays.Add(new() { Id = Guid.NewGuid(), HabitId = h.Id, Date = today.AddDays(-2), Status = HabitDayStatus.Full });
        await db.SaveChangesAsync();
        var controller = Controller();
        await controller.Update(h.Id, new("Read", h.Color, "FREQ=WEEKLY;BYDAY=MO,WE", null, null, null), today);
        Assert.Equal("FREQ=DAILY", HabitProgress.ScheduleOn(h, today).Rule);
        Assert.Equal("FREQ=WEEKLY;BYDAY=MO,WE", HabitProgress.ScheduleOn(h, today.AddDays(1)).Rule);
        await controller.Update(h.Id, new("Read renamed", h.Color, "FREQ=WEEKLY;BYDAY=TU", null, null, null), today);
        Assert.Equal(2, await db.HabitSchedules.CountAsync());
        Assert.Single(await db.HabitDays.ToListAsync());
        Assert.Equal("FREQ=DAILY", HabitProgress.ScheduleOn(h, today.AddDays(-2)).Rule);
        Assert.Equal("FREQ=WEEKLY;BYDAY=TU", HabitProgress.ScheduleOn(h, today.AddDays(1)).Rule);
    }

    [Fact]
    public async Task WeeklyGoal_ChangesNextMonday_AndCanCancelPendingChange()
    {
        var h = AddHabit("FREQ=WEEKLY", 3);
        await Controller().Update(h.Id, new("Read", h.Color, "FREQ=WEEKLY", 4, null, null), today);
        Assert.Equal(3, HabitProgress.ScheduleOn(h, today).Goal);
        Assert.Equal(4, HabitProgress.ScheduleOn(h, HabitProgress.Monday(today).AddDays(7)).Goal);
        await Controller().Update(h.Id, new("Read", h.Color, "FREQ=WEEKLY", 3, null, null), today);
        Assert.Single(await db.HabitSchedules.ToListAsync());
    }

    [Fact]
    public async Task PauseArchiveAndRestore_AreIdempotent_AndKeepHistory()
    {
        var h = AddHabit();
        var controller = Controller();
        await controller.SetDayStatus(h.Id, today.AddDays(-1), new("full"), today);
        await controller.SetState(h.Id, new("paused"), today);
        await controller.SetState(h.Id, new("paused"), today);
        await controller.SetState(h.Id, new("archived"), today);
        Assert.Single(await db.HabitInactivePeriods.ToListAsync());
        Assert.IsType<BadRequestResult>((await controller.SetDayStatus(h.Id, today, new("full"), today)).Result);
        await controller.SetState(h.Id, new("active"), today);
        Assert.True(HabitProgress.IsScheduled(h, today));
        Assert.Single(await db.HabitDays.ToListAsync());
        Assert.Equal(today, (await db.HabitInactivePeriods.SingleAsync()).EndDate);
    }

    [Fact]
    public async Task Marks_RejectFuturePreTrackingAndInvalidStatuses_AndRemainOnePerDay()
    {
        var h = AddHabit(start: today);
        var controller = Controller();
        foreach (var (date, status) in new[] { (today.AddDays(1), "full"), (today.AddDays(-1), "skip"), (today, "2") })
            Assert.IsType<BadRequestResult>((await controller.SetDayStatus(h.Id, date, new(status), today)).Result);
        await controller.SetDayStatus(h.Id, today, new("full"), today);
        await controller.SetDayStatus(h.Id, today, new("skip"), today);
        Assert.Equal(HabitDayStatus.Skip, (await db.HabitDays.SingleAsync()).Status);
        await controller.SetDayStatus(h.Id, today, new("none"), today);
        Assert.Empty(await db.HabitDays.ToListAsync());
    }

    [Fact]
    public async Task AllNewEndpoints_EnforceOwnership()
    {
        var h = AddHabit();
        var stranger = Controller(Guid.NewGuid());
        Assert.IsType<NotFoundResult>((await stranger.History(h.Id)).Result);
        Assert.IsType<NotFoundResult>((await stranger.SetState(h.Id, new("archived"))).Result);
        Assert.IsType<NotFoundResult>((await stranger.SetDayStatus(h.Id, today, new("full"))).Result);
        Assert.IsType<NotFoundResult>((await stranger.Update(h.Id, new("Other", h.Color, "FREQ=DAILY", null, null, null))).Result);
        var result = Assert.IsType<OkObjectResult>((await stranger.GetAll()).Result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<HabitWithHistoryDTO>>(result.Value));
    }

    [Fact]
    public async Task History_UsesCalendarRangesEvenWhenThereAreNoMarks()
    {
        var h = AddHabit(start: today.AddDays(-200));
        var page = (await Controller().History(h.Id, today.AddDays(-60), 56, today)).Value!;
        Assert.Empty(page.Items);
        Assert.Equal(today.AddDays(-61), page.To);
        Assert.Equal(today.AddDays(-116), page.From);
        Assert.True(page.HasEarlier);
    }

    [Theory]
    [InlineData("FREQ=DAILY", 0)]
    [InlineData("FREQ=DAILY", 8)]
    [InlineData("FREQ=MONTHLY", null)]
    [InlineData("FREQ=WEEKLY;BYDAY=XX", null)]
    public void ScheduleValidation_RejectsUnsupportedValues(string rule, int? goal) =>
        Assert.False(HabitProgress.TryNormalizeSchedule(rule, goal, out _));

    [Fact]
    public async Task Migration_BackfillsExistingHabitsWithoutChangingMarks()
    {
        using var cn = new SqliteConnection("Data Source=:memory:");
        await cn.OpenAsync();
        await using var migrated = new ApplicationContext(new DbContextOptionsBuilder<ApplicationContext>().UseSqlite(cn).Options);
        await migrated.GetService<IMigrator>().MigrateAsync("20261028183634_AddDayFeedback");
        var habitId = Guid.NewGuid(); var markId = Guid.NewGuid();
        var emptyId = Guid.NewGuid();
        await migrated.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Users (Id, Name, Email, PasswordHash) VALUES ({userId}, 'Test', 'legacy@example.test', 'unused')");
        await migrated.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Habits (Id, UserId, Title, Color, RecurrenceRule) VALUES ({habitId}, {userId}, 'Legacy', '#52b58b', 'FREQ=DAILY')");
        await migrated.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Habits (Id, UserId, Title, Color, RecurrenceRule) VALUES ({emptyId}, {userId}, 'No history yet', '#52b58b', 'FREQ=DAILY')");
        await migrated.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO HabitDays (Id, HabitId, Date, Status) VALUES ({markId}, {habitId}, '2026-01-15', 2)");
        await migrated.Database.MigrateAsync();
        Assert.False(migrated.Database.HasPendingModelChanges());
        var habit = await migrated.Habits.Include(h => h.Schedules).SingleAsync(h => h.Id == habitId);
        Assert.Equal(Day("2026-01-15"), habit.StartDate);
        Assert.Equal(HabitState.Active, habit.State);
        var initial = Assert.Single(habit.Schedules);
        Assert.Equal(habit.StartDate, initial.EffectiveFrom);
        Assert.NotNull(await migrated.HabitSchedules.SingleOrDefaultAsync(s => s.Id == initial.Id));
        var empty = await migrated.Habits.SingleAsync(h => h.Id == emptyId);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), empty.StartDate);
        var mark = await migrated.HabitDays.SingleAsync();
        Assert.Equal(markId, mark.Id);
        Assert.Equal(HabitDayStatus.Full, mark.Status);
    }

    public void Dispose() { db.Dispose(); connection.Dispose(); }
}
