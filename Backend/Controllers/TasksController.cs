using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/tasks")]
public class TasksController(ApplicationContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TaskDTO>>> GetAll()
    {
        var userId = User.GetUserId();
        var tasks = await context.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => t.ToDTO())
            .ToListAsync();

        return tasks;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDTO>> Get(Guid id)
    {
        var userId = User.GetUserId();
        var task = await context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (task is null)
            return NotFound();

        return task.ToDTO();
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskDTO), StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskDTO>> Create(CreateTaskRequest request)
    {
        var userId = User.GetUserId();
        if (request.GoalId.HasValue)
        {
            var goalExists = await context.Goals.AnyAsync(x => x.Id == request.GoalId.Value && x.UserId == userId);
            if (!goalExists)
                return BadRequest();
        }
        if (request.LifeAreaId.HasValue)
        {
            var lifeAreaExists = await context.LifeAreas.AnyAsync(x => x.Id == request.LifeAreaId.Value && x.UserId == userId);
            if (!lifeAreaExists)
                return BadRequest();
        }

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            RecurrenceRule = request.RecurrenceRule,
            GoalId = request.GoalId,
            LifeAreaId = request.LifeAreaId
        };

        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = task.Id }, task.ToDTO());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDTO>> Update(Guid id, UpdateTaskRequest request)
    {
        var userId = User.GetUserId();
        var task = await context.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (task is null)
            return NotFound();

        if (request.Title is null)
            return BadRequest();

        if (request.GoalId.HasValue)
        {
            var goalExists = await context.Goals.AnyAsync(x => x.Id == request.GoalId.Value && x.UserId == userId);
            if (!goalExists)
                return BadRequest();
        }

        if (request.LifeAreaId.HasValue)
        {
            var lifeAreaExists = await context.LifeAreas.AnyAsync(x => x.Id == request.LifeAreaId.Value && x.UserId == userId);
            if (!lifeAreaExists)
                return BadRequest();
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.DueDate = request.DueDate;
        task.CompletionDate = request.CompletionDate;
        task.RecurrenceRule = request.RecurrenceRule;
        task.GoalId = request.GoalId;
        task.LifeAreaId = request.LifeAreaId;
        task.SortOrder = request.SortOrder;

        await context.SaveChangesAsync();

        return task.ToDTO();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        var task = await context.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (task is null)
            return NotFound();

        context.Tasks.Remove(task);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
