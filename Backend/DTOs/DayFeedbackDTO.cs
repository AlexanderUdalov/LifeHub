using System.ComponentModel.DataAnnotations;
using LifeHub.Models;

namespace LifeHub.DTOs;

public record DayFeedbackDTO(DateOnly Date, int Score, string? Note, DateTimeOffset UpdatedAt);

public class SaveDayFeedbackRequest
{
    [Required, Range(0, 10)]
    public int? Score { get; init; }

    [StringLength(1000)]
    public string? Note { get; init; }
}

public static class DayFeedbackMapping
{
    public static DayFeedbackDTO ToDTO(this DayFeedback feedback) =>
        new(feedback.Date, feedback.Score, feedback.Note, feedback.UpdatedAt);
}
