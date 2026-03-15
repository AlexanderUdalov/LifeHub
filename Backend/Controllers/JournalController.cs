using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/journal")]
public class JournalController(ApplicationContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<JournalEntryDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<JournalEntryDTO>>> GetAll()
    {
        var userId = User.GetUserId();
        // SQLite does not support DateTimeOffset in Select/OrderBy; load entities then project and sort in memory
        var entries = await context.JournalEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .ToListAsync();

        return entries
            .Select(e => e.ToDTO())
            .OrderByDescending(e => e.IsPinned)
            .ThenByDescending(e => e.PinnedAt ?? e.CreatedAt)
            .ThenByDescending(e => e.CreatedAt)
            .ToList();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JournalEntryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JournalEntryDTO>> Get(Guid id)
    {
        var userId = User.GetUserId();

        var entry = await context.JournalEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry is null)
            return NotFound();

        return entry.ToDTO();
    }

    [HttpPost]
    [ProducesResponseType(typeof(JournalEntryDTO), StatusCodes.Status201Created)]
    public async Task<ActionResult<JournalEntryDTO>> Create(CreateJournalEntryRequest request)
    {
        var userId = User.GetUserId();

        var now = DateTimeOffset.UtcNow;

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Text = request.Text,
            CreatedAt = now,
            UpdatedAt = null,
            IsPinned = false,
            PinnedAt = null,
            TaskItemId = request.TaskItemId,
            HabitId = request.HabitId,
            AddictionId = request.AddictionId,
            GoalId = request.GoalId,
            LifeAreaId = request.LifeAreaId
        };

        context.JournalEntries.Add(entry);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = entry.Id }, entry.ToDTO());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(JournalEntryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JournalEntryDTO>> Update(Guid id, UpdateJournalEntryRequest request)
    {
        var userId = User.GetUserId();

        var entry = await context.JournalEntries
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry is null)
            return NotFound();

        if (request.Text is not null)
        {
            entry.Text = request.Text;
        }

        if (request.CreatedAt.HasValue)
        {
            entry.CreatedAt = request.CreatedAt.Value;
        }

        if (request.IsPinned.HasValue)
        {
            var now = DateTimeOffset.UtcNow;
            if (!entry.IsPinned && request.IsPinned.Value)
            {
                entry.PinnedAt = now;
            }
            else if (!request.IsPinned.Value)
            {
                entry.PinnedAt = null;
            }

            entry.IsPinned = request.IsPinned.Value;
        }

        if (request.TaskItemId.HasValue)
        {
            entry.TaskItemId = request.TaskItemId;
        }

        if (request.HabitId.HasValue)
        {
            entry.HabitId = request.HabitId;
        }

        if (request.AddictionId.HasValue)
        {
            entry.AddictionId = request.AddictionId;
        }

        entry.GoalId = request.GoalId;
        entry.LifeAreaId = request.LifeAreaId;

        entry.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();

        return entry.ToDTO();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();

        var entry = await context.JournalEntries
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry is null)
            return NotFound();

        context.JournalEntries.Remove(entry);
        await context.SaveChangesAsync();

        return NoContent();
    }

}

