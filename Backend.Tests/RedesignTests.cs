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

public class RedesignTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly ApplicationContext context;
    private readonly Guid userId = Guid.NewGuid();
    private readonly Guid otherId = Guid.NewGuid();

    public RedesignTests()
    {
        connection.Open();
        context = new(new DbContextOptionsBuilder<ApplicationContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        foreach (var id in new[] { userId, otherId })
            context.Users.Add(new() { Id=id, Name="Test", Email=$"{id}@example.test", PasswordHash="unused" });
        context.SaveChanges();
    }

    [Fact]
    public async Task CalendarHistory_FiltersDateAndOwnerBeforePagination()
    {
        var day = new DateTimeOffset(2026,10,28,0,0,0,TimeSpan.Zero);
        for (var i=0; i<25; i++) context.Tasks.Add(new() {
            Id=Guid.NewGuid(), UserId=userId, Title=$"Task {i}", DueDate=day,
            CompletionDate=day.AddDays(i+1)
        });
        context.Tasks.Add(new() { Id=Guid.NewGuid(), UserId=userId, Title="Another date", DueDate=day.AddDays(1), CompletionDate=day.AddDays(30) });
        context.Tasks.Add(new() { Id=Guid.NewGuid(), UserId=otherId, Title="Private", DueDate=day, CompletionDate=day.AddDays(31) });
        await context.SaveChangesAsync();
        var controller = new TasksController(context) { ControllerContext = ForUser(userId) };
        var result = (await controller.GetCompleted(5,20,new DateOnly(2026,10,28))).Value!;
        Assert.Equal(25,result.Total);
        Assert.Equal(5,result.Items.Count);
        Assert.All(result.Items,task => Assert.StartsWith("Task ",task.Title));
        Assert.DoesNotContain(result.Items,task => task.Title is "Private" or "Another date");
    }

    [Fact]
    public async Task ReopenGoal_PreservesRelationsAndIsOwnerScoped()
    {
        var goal = new Goal { Id=Guid.NewGuid(), UserId=userId, Title="Finished", DueDate=DateTime.UtcNow, CompletedAt=DateTime.UtcNow };
        context.Goals.Add(goal);
        var task = new TaskItem { Id=Guid.NewGuid(), UserId=userId, Title="Linked", GoalId=goal.Id };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        Assert.IsType<NotFoundResult>((await new GoalsController(context) { ControllerContext=ForUser(otherId) }.Reopen(goal.Id)).Result);
        Assert.NotNull(goal.CompletedAt);
        var controller = new GoalsController(context) { ControllerContext=ForUser(userId) };
        Assert.IsType<OkObjectResult>((await controller.Reopen(goal.Id)).Result);
        Assert.Null(goal.CompletedAt);
        Assert.Equal(goal.Id,task.GoalId);
        Assert.IsType<OkObjectResult>((await controller.Reopen(goal.Id)).Result);
    }

    private static ControllerContext ForUser(Guid id) => new() { HttpContext=new DefaultHttpContext {
        User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,id.ToString())],"Test"))
    }};
    public void Dispose() { context.Dispose(); connection.Dispose(); }
}
