using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/habits")]
public class HabitsController(ApplicationContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<HabitWithHistoryDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HabitWithHistoryDTO>>> GetAll([FromQuery] int days = 14)
    {
        days = Math.Clamp(days, 1, 365);

        var userId = User.GetUserId();
        var habits = await context.Habits
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .ToListAsync();

        var habitIds = habits.Select(h => h.Id).ToArray();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = today.AddDays(-(days - 1));

        var historyEntries = await context.HabitDays
            .AsNoTracking()
            .Where(d => habitIds.Contains(d.HabitId) && d.Date >= startDate && d.Date <= today)
            .ToListAsync();

        var historyByHabit = historyEntries
            .GroupBy(d => d.HabitId)
            .ToDictionary(g => g.Key, g => g.Select(d => d.ToDTO()).ToList() as IReadOnlyList<HabitDayDTO>);

        var result = habits.Select(h =>
            new HabitWithHistoryDTO(
                h.ToDTO(),
                historyByHabit.GetValueOrDefault(h.Id, [])
            ));

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(HabitDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HabitDTO>> Create([FromBody] HabitUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.RecurrenceRule))
            return BadRequest();

        var userId = User.GetUserId();
        var habit = new Habit
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            Color = (request.Color ?? "").Trim().Length > 0 ? request.Color!.Trim() : "#3b82f6",
            RecurrenceRule = request.RecurrenceRule.Trim(),
            GoalId = request.GoalId
        };

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = habit.Id }, habit.ToDTO());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HabitDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HabitDTO>> Get(Guid id)
    {
        var userId = User.GetUserId();
        var habit = await context.Habits
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

        if (habit is null)
            return NotFound();

        return habit.ToDTO();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(HabitDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HabitDTO>> Update(Guid id, [FromBody] HabitUpsertRequest request)
    {
        var userId = User.GetUserId();
        var habit = await context.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

        if (habit is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.RecurrenceRule))
            return BadRequest();

        habit.Title = request.Title.Trim();
        habit.Color = (request.Color ?? "").Trim().Length > 0 ? request.Color!.Trim() : habit.Color;
        habit.RecurrenceRule = request.RecurrenceRule.Trim();
        habit.GoalId = request.GoalId;

        await context.SaveChangesAsync();
        return habit.ToDTO();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        var habit = await context.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

        if (habit is null)
            return NotFound();

        context.Habits.Remove(habit);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/days/{date}")]
    [ProducesResponseType(typeof(HabitDayDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HabitDayDTO>> SetDayStatus(Guid id, DateOnly date, [FromBody] SetDayStatusRequest request)
    {
        var userId = User.GetUserId();
        var habitExists = await context.Habits.AnyAsync(x => x.Id == id && x.UserId == userId);
        if (!habitExists)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest();

        if (!Enum.TryParse<HabitDayStatus>(request.Status, ignoreCase: true, out var status))
            return BadRequest();

        var existing = await context.HabitDays.FirstOrDefaultAsync(x => x.HabitId == id && x.Date == date);

        if (status == HabitDayStatus.None)
        {
            if (existing is not null)
            {
                context.HabitDays.Remove(existing);
                await context.SaveChangesAsync();
            }

            return Ok(new HabitDayDTO(date, "none"));
        }

        if (existing is null)
        {
            existing = new HabitDay
            {
                Id = Guid.NewGuid(),
                HabitId = id,
                Date = date,
                Status = status
            };
            context.HabitDays.Add(existing);
        }
        else
        {
            existing.Status = status;
        }

        await context.SaveChangesAsync();
        return Ok(existing.ToDTO());
    }
}
