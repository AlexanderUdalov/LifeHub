using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/goals")]
public class GoalsController(ApplicationContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GoalDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<GoalDTO>>> GetAll()
    {
        var userId = User.GetUserId();
        var goals = await context.Goals
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.DueDate)
            .Select(x => x.ToDTO())
            .ToListAsync();

        return Ok(goals);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GoalDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GoalDTO>> Get(Guid id)
    {
        var userId = User.GetUserId();
        var goal = await context.Goals
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (goal is null)
            return NotFound();

        return Ok(goal.ToDTO());
    }

    [HttpPost]
    [ProducesResponseType(typeof(GoalDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GoalDTO>> Create([FromBody] CreateGoalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest();

        var userId = User.GetUserId();
        if (request.LifeAreaId.HasValue)
        {
            var lifeAreaExists = await context.LifeAreas.AnyAsync(x => x.Id == request.LifeAreaId.Value && x.UserId == userId);
            if (!lifeAreaExists)
                return BadRequest();
        }

        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            DueDate = request.DueDate.UtcDateTime,
            LifeAreaId = request.LifeAreaId
        };

        context.Goals.Add(goal);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = goal.Id }, goal.ToDTO());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GoalDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GoalDTO>> Update(Guid id, [FromBody] UpdateGoalRequest request)
    {
        var userId = User.GetUserId();
        var goal = await context.Goals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (goal is null)
            return NotFound();

        if (request.Title is not null)
        {
            var title = request.Title.Trim();
            if (title.Length == 0)
                return BadRequest();
            goal.Title = title;
        }

        if (request.Description is not null)
            goal.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        if (request.DueDate.HasValue)
            goal.DueDate = request.DueDate.Value.UtcDateTime;

        if (request.LifeAreaId.HasValue)
        {
            var lifeAreaExists = await context.LifeAreas.AnyAsync(x => x.Id == request.LifeAreaId.Value && x.UserId == userId);
            if (!lifeAreaExists)
                return BadRequest();
        }
        goal.LifeAreaId = request.LifeAreaId;

        await context.SaveChangesAsync();
        return Ok(goal.ToDTO());
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        var goal = await context.Goals.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (goal is null)
            return NotFound();

        context.Goals.Remove(goal);
        await context.SaveChangesAsync();
        return NoContent();
    }
}
