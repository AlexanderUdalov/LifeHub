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
    /// <summary>
    /// Returns only active (non-completed) tasks. Use GET completed for completed tasks with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TaskDTO>>> GetAll()
    {
        var userId = User.GetUserId();
        // SQLite does not support DateTimeOffset in WHERE; filter in memory
        var tasks = await context.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync();

        return tasks
            .Where(t => t.CompletionDate == null)
            .Select(t => t.ToDTO())
            .ToList();
    }

    /// <summary>
    /// Returns paginated completed tasks for lazy loading. Sorted by completion date descending.
    /// </summary>
    [HttpGet("completed")]
    [ProducesResponseType(typeof(CompletedTasksPageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompletedTasksPageResponse>> GetCompleted(
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(0, offset);

        var userId = User.GetUserId();
        // SQLite does not support DateTimeOffset in WHERE/ORDER BY; load and sort in memory
        var all = await context.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync();

        var completed = all
            .Where(t => t.CompletionDate != null)
            .OrderByDescending(t => t.CompletionDate)
            .ToList();

        var total = completed.Count;
        var items = completed
            .Skip(offset)
            .Take(limit)
            .Select(t => t.ToDTO())
            .ToList();

        return new CompletedTasksPageResponse(items, total);
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
