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
        var task = await context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (task is null)
            return NotFound();

        return task.ToDTO();
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskDTO), StatusCodes.Status201Created)]
    public async Task<ActionResult<TaskDTO>> Create(CreateTaskRequest request)
    {
        var userId = User.GetUserId();
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            GoalId = request.GoalId
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
        var task = await context.Tasks.FirstOrDefaultAsync(x => x.Id == id);

        if (task is null)
            return NotFound();

        task.Title = request.Title ?? task.Title;
        task.Description = request.Description ?? task.Description;
        task.DueDate = request.DueDate ?? task.DueDate;
        task.CompletionDate = request.CompletionDate ?? task.CompletionDate;
        task.GoalId = request.GoalId ?? task.GoalId;

        await context.SaveChangesAsync();

        return task.ToDTO();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await context.Tasks.FirstOrDefaultAsync(x => x.Id == id);

        if (task is null)
            return NotFound();

        context.Tasks.Remove(task);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
