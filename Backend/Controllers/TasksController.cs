using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private static readonly List<TaskItem> _tasks = [];

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskDTO>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TaskDTO>> GetAll()
    {
        return _tasks.Select(t => t.ToDTO()).ToList();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TaskDTO> Get(Guid id)
    {
        var task = _tasks.FirstOrDefault(x => x.Id == id);
        if (task is null)
            return NotFound();

        return task.ToDTO();
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskDTO), StatusCodes.Status201Created)]
    public ActionResult<TaskDTO> Create(CreateTaskRequest request)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(), // позже из auth
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            GoalId = request.GoalId
        };

        _tasks.Add(task);

        return CreatedAtAction(nameof(Get), new { id = task.Id }, task.ToDTO());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TaskDTO> Update(Guid id, UpdateTaskRequest request)
    {
        var task = _tasks.FirstOrDefault(x => x.Id == id);
        if (task is null)
            return NotFound();

        task.Title = request.Title;
        task.Description = request.Description;
        task.DueDate = request.DueDate;
        task.CompletionDate = request.CompletionDate;
        task.GoalId = request.GoalId;

        return task.ToDTO();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Delete(Guid id)
    {
        var task = _tasks.FirstOrDefault(x => x.Id == id);
        if (task is null)
            return NotFound();

        _tasks.Remove(task);
        return NoContent();
    }
}
