using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/addictions")]
public class AddictionsController(ApplicationContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AddictionWithResetsDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AddictionWithResetsDTO>>> GetAll([FromQuery] int days = 14)
    {
        days = Math.Clamp(days, 1, 365);

        var userId = User.GetUserId();
        var addictions = await context.Addictions
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync();

        var addictionIds = addictions.Select(a => a.Id).ToArray();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = today.AddDays(-(days - 1));

        var resets = await context.AddictionResets
            .AsNoTracking()
            .Where(r => addictionIds.Contains(r.AddictionId) && r.Date >= startDate && r.Date <= today)
            .OrderBy(r => r.Date).ThenBy(r => r.ResetAt)
            .ToListAsync();

        var lastResetByAddiction = await context.AddictionResets
            .AsNoTracking()
            .Where(r => addictionIds.Contains(r.AddictionId))
            .GroupBy(r => r.AddictionId)
            .Select(g => new { AddictionId = g.Key, LastResetAt = g.Max(r => r.ResetAt) })
            .ToDictionaryAsync(x => x.AddictionId, x => (DateTime?)x.LastResetAt);

        var resetsByAddiction = resets
            .GroupBy(r => r.AddictionId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.ResetAt).Select(r => r.Date).ToList() as IReadOnlyList<DateOnly>);

        var result = addictions.Select(a =>
            new AddictionWithResetsDTO(
                a.ToDTO(),
                resetsByAddiction.GetValueOrDefault(a.Id, []),
                lastResetByAddiction.GetValueOrDefault(a.Id)
            ));

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddictionDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddictionDTO>> Create([FromBody] AddictionUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest();

        var userId = User.GetUserId();
        var addiction = new Addiction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            Color = request.Color.Trim(),
            CreatedAt = DateTime.UtcNow,
            GoalId = request.GoalId
        };

        context.Addictions.Add(addiction);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = addiction.Id }, addiction.ToDTO());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AddictionDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddictionDTO>> Get(Guid id)
    {
        var userId = User.GetUserId();
        var addiction = await context.Addictions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (addiction is null)
            return NotFound();

        return addiction.ToDTO();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AddictionDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddictionDTO>> Update(Guid id, [FromBody] AddictionUpsertRequest request)
    {
        var userId = User.GetUserId();
        var addiction = await context.Addictions.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (addiction is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest();

        addiction.Title = request.Title.Trim();
        addiction.Color = request.Color.Trim();
        addiction.GoalId = request.GoalId;

        await context.SaveChangesAsync();
        return addiction.ToDTO();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        var addiction = await context.Addictions.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (addiction is null)
            return NotFound();

        context.Addictions.Remove(addiction);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/resets/{date}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetReset(Guid id, DateOnly date)
    {
        var userId = User.GetUserId();
        var exists = await context.Addictions.AnyAsync(a => a.Id == id && a.UserId == userId);
        if (!exists)
            return NotFound();

        context.AddictionResets.Add(new AddictionReset
        {
            Id = Guid.NewGuid(),
            AddictionId = id,
            Date = date,
            ResetAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id:guid}/resets/{date}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveReset(Guid id, DateOnly date)
    {
        var userId = User.GetUserId();
        var exists = await context.Addictions.AnyAsync(a => a.Id == id && a.UserId == userId);
        if (!exists)
            return NotFound();

        var existing = await context.AddictionResets
            .Where(r => r.AddictionId == id && r.Date == date)
            .OrderByDescending(r => r.ResetAt)
            .FirstOrDefaultAsync();
        if (existing is not null)
        {
            context.AddictionResets.Remove(existing);
            await context.SaveChangesAsync();
        }

        return NoContent();
    }
}
