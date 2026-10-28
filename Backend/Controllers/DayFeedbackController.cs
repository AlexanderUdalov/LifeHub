using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/day-feedback")]
public class DayFeedbackController(ApplicationContext context) : ControllerBase
{
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(DayFeedbackDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<DayFeedbackDTO>> Get(DateOnly date)
    {
        var userId = User.GetUserId();
        var feedback = await context.DayFeedbacks.AsNoTracking()
            .SingleOrDefaultAsync(f => f.UserId == userId && f.Date == date);

        if (feedback is null) return NoContent();
        return feedback.ToDTO();
    }

    [HttpPut("{date}")]
    [ProducesResponseType(typeof(DayFeedbackDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DayFeedbackDTO>> Save(DateOnly date, SaveDayFeedbackRequest request)
    {
        var userId = User.GetUserId();
        var feedback = await context.DayFeedbacks
            .SingleOrDefaultAsync(f => f.UserId == userId && f.Date == date);

        if (feedback is null)
        {
            feedback = new DayFeedback { Id = Guid.NewGuid(), UserId = userId, Date = date };
            context.DayFeedbacks.Add(feedback);
        }

        feedback.Score = request.Score!.Value;
        feedback.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        feedback.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        return feedback.ToDTO();
    }
}
