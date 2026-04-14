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
public class AddictionsController(
    ApplicationContext context,
    AddictionTriggerGuidanceService triggerGuidanceService) : ControllerBase
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
        var startAtUtc = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var resets = await context.AddictionResets
            .AsNoTracking()
            .Include(r => r.JournalEntry)
            .Where(r => addictionIds.Contains(r.AddictionId) && r.Date >= startDate && r.Date <= today)
            .OrderBy(r => r.Date).ThenBy(r => r.ResetAt)
            .ToListAsync();

        var lastResetByAddiction = await context.AddictionResets
            .AsNoTracking()
            .Where(r => addictionIds.Contains(r.AddictionId))
            .GroupBy(r => r.AddictionId)
            .Select(g => new { AddictionId = g.Key, LastResetAt = g.Max(r => r.ResetAt) })
            .ToDictionaryAsync(x => x.AddictionId, x => (DateTime?)x.LastResetAt);

        var triggerEvents = await context.AddictionTriggerEvents
            .AsNoTracking()
            .Include(t => t.JournalEntry)
            .Where(t => addictionIds.Contains(t.AddictionId) && t.EventAt >= startAtUtc)
            .OrderByDescending(t => t.EventAt)
            .ToListAsync();

        var resetsByAddiction = resets
            .GroupBy(r => r.AddictionId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(r => r.ResetAt)
                    .Select(r => new AddictionResetEntryDTO(
                        r.Id,
                        r.Date,
                        r.ResetAt,
                        r.JournalEntryId,
                        r.JournalEntry?.Text))
                    .ToList() as IReadOnlyList<AddictionResetEntryDTO>);

        var triggerEventsByAddiction = triggerEvents
            .GroupBy(t => t.AddictionId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.EventAt)
                    .Select(t => new AddictionTriggerEventDTO(
                        t.Id,
                        t.EventAt,
                        t.Outcome,
                        t.Note,
                        t.JournalEntryId,
                        t.JournalEntry?.Text))
                    .ToList() as IReadOnlyList<AddictionTriggerEventDTO>);

        var result = addictions.Select(a =>
            new AddictionWithResetsDTO(
                a.ToDTO(),
                resetsByAddiction.GetValueOrDefault(a.Id, []),
                triggerEventsByAddiction.GetValueOrDefault(a.Id, []),
                lastResetByAddiction.GetValueOrDefault(a.Id),
                AddictionMapping.CalculateCurrentStreakDays(
                    a,
                    lastResetByAddiction.GetValueOrDefault(a.Id)
                )
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

        var addiction = new Addiction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            Description = NormalizeOptionalText(request.Description),
            Color = request.Color.Trim(),
            CreatedAt = DateTime.UtcNow,
            GoalId = request.GoalId,
            LifeAreaId = request.LifeAreaId
        };

        context.Addictions.Add(addiction);
        await context.SaveChangesAsync();

        if (request.LastRelapseAt.HasValue)
        {
            var resetAt = ToUtc(request.LastRelapseAt.Value);
            var date = DateOnly.FromDateTime(resetAt);
            context.AddictionResets.Add(new AddictionReset
            {
                Id = Guid.NewGuid(),
                AddictionId = addiction.Id,
                Date = date,
                ResetAt = resetAt,
                JournalEntryId = null
            });
            await context.SaveChangesAsync();
        }

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

        addiction.Title = request.Title.Trim();
        addiction.Description = NormalizeOptionalText(request.Description);
        addiction.Color = request.Color.Trim();
        addiction.GoalId = request.GoalId;
        addiction.LifeAreaId = request.LifeAreaId;

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
    public async Task<IActionResult> SetReset(Guid id, DateOnly date, [FromBody] SetResetRequest? body)
    {
        var addiction = await context.Addictions.FirstOrDefaultAsync(a => a.Id == id && a.UserId == User.GetUserId());
        if (addiction is null)
            return NotFound();

        DateTime resetAt;
        DateOnly dateOnly;
        if (body?.ResetAt is { } clientAt)
        {
            resetAt = ToUtc(clientAt);
            dateOnly = DateOnly.FromDateTime(resetAt);
        }
        else
        {
            dateOnly = date;
            resetAt = DateTime.UtcNow;
        }

        Guid? journalEntryId = null;
        var note = NormalizeOptionalText(body?.Note);
        if (note is not null)
        {
            var entry = CreateJournalEntry(addiction, note, resetAt);
            context.JournalEntries.Add(entry);
            journalEntryId = entry.Id;
        }

        context.AddictionResets.Add(new AddictionReset
        {
            Id = Guid.NewGuid(),
            AddictionId = id,
            Date = dateOnly,
            ResetAt = resetAt,
            JournalEntryId = journalEntryId
        });
        await context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id:guid}/trigger-events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogTriggerEvent(
        Guid id,
        [FromBody] LogTriggerEventRequest request,
        [FromQuery] string? language = null)
    {
        var addiction = await context.Addictions
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == User.GetUserId());
        if (addiction is null)
            return NotFound();

        var eventAt = request.EventAt.HasValue
            ? ToUtc(request.EventAt.Value)
            : DateTime.UtcNow;

        var note = NormalizeOptionalText(request.Note);
        Guid? journalEntryId = null;
        if (note is not null)
        {
            var journalText = BuildTriggerJournalText(request.Outcome, note, NormalizeLanguage(language));
            var entry = CreateJournalEntry(addiction, journalText, eventAt);
            context.JournalEntries.Add(entry);
            journalEntryId = entry.Id;
        }

        context.AddictionTriggerEvents.Add(new AddictionTriggerEvent
        {
            Id = Guid.NewGuid(),
            AddictionId = id,
            EventAt = eventAt,
            Outcome = request.Outcome,
            Note = note,
            JournalEntryId = journalEntryId
        });

        if (request.Outcome == AddictionTriggerOutcome.Relapsed)
        {
            context.AddictionResets.Add(new AddictionReset
            {
                Id = Guid.NewGuid(),
                AddictionId = id,
                Date = DateOnly.FromDateTime(eventAt),
                ResetAt = eventAt,
                JournalEntryId = journalEntryId
            });
        }

        await context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("{id:guid}/trigger-guidance")]
    [ProducesResponseType(typeof(GenerateTriggerGuidanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GenerateTriggerGuidanceResponse>> GetTriggerGuidance(Guid id, [FromQuery] string? language = null)
    {
        var userId = User.GetUserId();
        var addiction = await context.Addictions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (addiction is null)
            return NotFound();

        var sinceUtc = DateTime.UtcNow.AddDays(-30);

        var recentResets = await context.AddictionResets
            .AsNoTracking()
            .Where(r => r.AddictionId == id && r.ResetAt >= sinceUtc)
            .OrderByDescending(r => r.ResetAt)
            .Take(20)
            .ToListAsync();

        var recentTriggerEvents = await context.AddictionTriggerEvents
            .AsNoTracking()
            .Where(t => t.AddictionId == id && t.EventAt >= sinceUtc)
            .OrderByDescending(t => t.EventAt)
            .Take(30)
            .ToListAsync();

        var lastResetAt = await context.AddictionResets
            .AsNoTracking()
            .Where(r => r.AddictionId == id)
            .OrderByDescending(r => r.ResetAt)
            .Select(r => (DateTime?)r.ResetAt)
            .FirstOrDefaultAsync();

        var guidance = await triggerGuidanceService.GenerateAsync(
            addiction,
            AddictionMapping.CalculateCurrentStreakDays(addiction, lastResetAt),
            recentResets,
            recentTriggerEvents,
            NormalizeLanguage(language));

        return Ok(guidance);
    }

    private static DateTime ToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return "ru";

        var normalized = language.Trim().ToLowerInvariant();
        return normalized.StartsWith("en", StringComparison.Ordinal) ? "en" : "ru";
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildTriggerJournalText(
        AddictionTriggerOutcome outcome,
        string note,
        string language)
    {
        var isEnglish = string.Equals(language, "en", StringComparison.Ordinal);
        var prefix = outcome == AddictionTriggerOutcome.Overcame
            ? (isEnglish ? "Trigger overcame" : "Триггер преодолён")
            : (isEnglish ? "Trigger relapsed" : "Срыв после триггера");
        return $"{prefix}: {note}";
    }

    private static JournalEntry CreateJournalEntry(Addiction addiction, string text, DateTime eventAtUtc)
    {
        var createdAt = new DateTimeOffset(DateTime.SpecifyKind(eventAtUtc, DateTimeKind.Utc), TimeSpan.Zero);
        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            UserId = addiction.UserId,
            Text = text,
            CreatedAt = createdAt,
            IsPinned = false,
            AddictionId = addiction.Id,
            GoalId = addiction.GoalId,
            LifeAreaId = addiction.LifeAreaId
        };
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
