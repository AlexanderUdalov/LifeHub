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
    private IQueryable<Habit> OwnedHabits => context.Habits
        .Include(h => h.Schedules).Include(h => h.InactivePeriods)
        .Where(h => h.UserId == User.GetUserId());

    // Calendar dates belong to the client, including around midnight on a UTC server.
    private static DateOnly LocalToday(DateOnly? today) => today ?? DateOnly.FromDateTime(DateTime.Today);
    private static bool ValidToday(DateOnly date) => Math.Abs(date.DayNumber - DateOnly.FromDateTime(DateTime.UtcNow).DayNumber) <= 1;
    private async Task<bool> ValidRelations(HabitUpsertRequest r) =>
        (!r.GoalId.HasValue || await context.Goals.AnyAsync(x => x.Id == r.GoalId && x.UserId == User.GetUserId())) &&
        (!r.LifeAreaId.HasValue || await context.LifeAreas.AnyAsync(x => x.Id == r.LifeAreaId && x.UserId == User.GetUserId()));

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<HabitWithHistoryDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<HabitWithHistoryDTO>>> GetAll([FromQuery] int days = 56, [FromQuery] DateOnly? today = null)
    {
        var date = LocalToday(today);
        if (!ValidToday(date)) return BadRequest();
        days = Math.Clamp(days, 1, 365);
        var habits = await OwnedHabits.AsNoTracking().AsSplitQuery().ToListAsync();
        var ids = habits.Select(h => h.Id).ToArray();
        var allDays = await context.HabitDays.AsNoTracking().Where(d => ids.Contains(d.HabitId) && d.Date <= date).ToListAsync();
        var grouped = allDays.GroupBy(d => d.HabitId).ToDictionary(g => g.Key, g => g.ToList());
        return Ok(habits.Select(h => {
            var history = grouped.GetValueOrDefault(h.Id, []);
            return new HabitWithHistoryDTO(h.ToDTO(date),
                history.Where(d => d.Date >= date.AddDays(1 - days)).OrderBy(d => d.Date).Select(d => d.ToDTO()).ToList(),
                HabitProgress.Streak(h, history, date));
        }).ToList());
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(HabitHistoryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HabitHistoryDTO>> History(Guid id, [FromQuery] DateOnly? before = null,
        [FromQuery] int days = 56, [FromQuery] DateOnly? today = null)
    {
        var date = LocalToday(today);
        if (!ValidToday(date) || before > date.AddDays(1)) return BadRequest();
        var habit = await OwnedHabits.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
        if (habit is null) return NotFound();
        var to = before?.AddDays(-1) ?? date;
        var from = to.AddDays(1 - Math.Clamp(days, 1, 90));
        if (from < habit.StartDate) from = habit.StartDate;
        var history = await context.HabitDays.AsNoTracking()
            .Where(d => d.HabitId == id && d.Date >= from && d.Date <= to).OrderBy(d => d.Date).ToListAsync();
        return new HabitHistoryDTO(history.Select(d => d.ToDTO()).ToList(), from, to, from > habit.StartDate);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HabitDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HabitDTO>> Get(Guid id, [FromQuery] DateOnly? today = null)
    {
        var date = LocalToday(today);
        if (!ValidToday(date)) return BadRequest();
        var habit = await OwnedHabits.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id);
        return habit is null ? NotFound() : habit.ToDTO(date);
    }

    [HttpPost]
    [ProducesResponseType(typeof(HabitDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HabitDTO>> Create([FromBody] HabitUpsertRequest request, [FromQuery] DateOnly? today = null)
    {
        var date = LocalToday(today);
        if (!ValidToday(date) || string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200 ||
            !HabitProgress.TryNormalizeSchedule(request.RecurrenceRule, request.TimesPerWeekGoal, out var rule) ||
            !await ValidRelations(request)) return BadRequest();
        var habit = new Habit { Id = Guid.NewGuid(), UserId = User.GetUserId(), Title = request.Title.Trim(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#52b58b" : request.Color.Trim(),
            RecurrenceRule = rule, TimesPerWeekGoal = request.TimesPerWeekGoal,
            GoalId = request.GoalId, LifeAreaId = request.LifeAreaId, StartDate = date };
        habit.Schedules.Add(new() { Id = Guid.NewGuid(), EffectiveFrom = date,
            RecurrenceRule = rule, TimesPerWeekGoal = request.TimesPerWeekGoal });
        context.Habits.Add(habit);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = habit.Id }, habit.ToDTO(date));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(HabitDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HabitDTO>> Update(Guid id, [FromBody] HabitUpsertRequest request, [FromQuery] DateOnly? today = null)
    {
        var habit = await OwnedHabits.FirstOrDefaultAsync(h => h.Id == id);
        if (habit is null) return NotFound();
        var date = LocalToday(today);
        if (!ValidToday(date) || string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200 ||
            !HabitProgress.TryNormalizeSchedule(request.RecurrenceRule, request.TimesPerWeekGoal, out var rule) ||
            !await ValidRelations(request)) return BadRequest();
        var current = HabitProgress.ScheduleOn(habit, date);
        HabitProgress.TryNormalizeSchedule(current.Rule, current.Goal, out var currentRule);
        // Keep the original schedule and all marks. Replace only a not-yet-effective change.
        context.HabitSchedules.RemoveRange(habit.Schedules.Where(s => s.EffectiveFrom > date));
        habit.Schedules.RemoveAll(s => s.EffectiveFrom > date);
        if (currentRule != rule || current.Goal != request.TimesPerWeekGoal)
        {
            var effective = current.Goal.HasValue || request.TimesPerWeekGoal.HasValue
                ? HabitProgress.Monday(date).AddDays(7) : date.AddDays(1);
            context.HabitSchedules.Add(new() { Id = Guid.NewGuid(), HabitId = habit.Id, EffectiveFrom = effective,
                RecurrenceRule = rule, TimesPerWeekGoal = request.TimesPerWeekGoal });
        }
        habit.Title = request.Title.Trim();
        if (!string.IsNullOrWhiteSpace(request.Color)) habit.Color = request.Color.Trim();
        habit.GoalId = request.GoalId;
        habit.LifeAreaId = request.LifeAreaId;
        await context.SaveChangesAsync();
        return habit.ToDTO(date);
    }

    [HttpPut("{id:guid}/state")]
    [ProducesResponseType(typeof(HabitStateUpdateDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HabitStateUpdateDTO>> SetState(Guid id, [FromBody] SetHabitStateRequest request, [FromQuery] DateOnly? today = null)
    {
        var habit = await OwnedHabits.FirstOrDefaultAsync(h => h.Id == id);
        if (habit is null) return NotFound();
        var date = LocalToday(today);
        if (!ValidToday(date) || request.State is not ("active" or "paused" or "archived")) return BadRequest();
        var state = Enum.Parse<HabitState>(request.State, true);
        if (state != habit.State)
        {
            var open = habit.InactivePeriods.FirstOrDefault(p => p.EndDate is null);
            if (open is not null && date < open.StartDate) return BadRequest();
            if (state == HabitState.Active && open is not null) open.EndDate = date;
            else if (state != HabitState.Active && open is null)
                context.HabitInactivePeriods.Add(new() { Id = Guid.NewGuid(), HabitId = habit.Id, StartDate = date });
            habit.State = state;
            await context.SaveChangesAsync();
        }
        var history = await context.HabitDays.AsNoTracking().Where(d => d.HabitId == id).ToListAsync();
        return Ok(new HabitStateUpdateDTO(habit.ToDTO(date), HabitProgress.Streak(habit, history, date)));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var habit = await OwnedHabits.FirstOrDefaultAsync(h => h.Id == id);
        if (habit is null) return NotFound();
        context.Habits.Remove(habit);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/days/{date}")]
    [ProducesResponseType(typeof(HabitDayUpdateDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HabitDayUpdateDTO>> SetDayStatus(Guid id, DateOnly date, [FromBody] SetDayStatusRequest request,
        [FromQuery] DateOnly? today = null)
    {
        var habit = await OwnedHabits.FirstOrDefaultAsync(h => h.Id == id);
        if (habit is null) return NotFound();
        var local = LocalToday(today);
        if (!ValidToday(local) || date > local || request.Status is not ("none" or "skip" or "full")) return BadRequest();
        var status = Enum.Parse<HabitDayStatus>(request.Status, true);
        var existing = await context.HabitDays.FirstOrDefaultAsync(x => x.HabitId == id && x.Date == date);
        if (status != HabitDayStatus.None && !HabitProgress.IsScheduled(habit, date)) return BadRequest();
        if (status == HabitDayStatus.None)
        {
            if (existing is not null) context.HabitDays.Remove(existing);
        }
        else if (existing is null)
            context.HabitDays.Add(new() { Id = Guid.NewGuid(), HabitId = id, Date = date, Status = status });
        else existing.Status = status;
        await context.SaveChangesAsync();
        var history = await context.HabitDays.AsNoTracking().Where(d => d.HabitId == id).ToListAsync();
        return Ok(new HabitDayUpdateDTO(date, request.Status, HabitProgress.Streak(habit, history, local)));
    }
}
