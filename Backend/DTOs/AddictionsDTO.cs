using LifeHub.Models;

namespace LifeHub.DTOs;

public record AddictionDTO(
    Guid Id,
    string Title,
    string Color,
    DateTime CreatedAt,
    Guid? GoalId,
    Guid? LifeAreaId
);

public record AddictionWithResetsDTO(
    AddictionDTO Addiction,
    IReadOnlyList<DateOnly> ResetDates,
    DateTime? LastResetAt,
    int CurrentStreakDays
);

public record AddictionUpsertRequest(
    string Title,
    string Color,
    Guid? GoalId,
    Guid? LifeAreaId,
    /// <summary>Optional. When creating, sets the last relapse date (adds one reset on this date). Ignored on update.</summary>
    DateOnly? LastRelapseDate
);

public static class AddictionMapping
{
    public static AddictionDTO ToDTO(this Addiction addiction) =>
        new(
            addiction.Id,
            addiction.Title,
            addiction.Color,
            addiction.CreatedAt,
            addiction.GoalId,
            addiction.LifeAreaId
        );

    public static int CalculateCurrentStreakDays(Addiction addiction, DateTime? lastResetAtUtc)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (lastResetAtUtc is { } lastResetAt)
        {
            var resetDate = DateOnly.FromDateTime(lastResetAt.ToUniversalTime());
            var diff = today.DayNumber - resetDate.DayNumber;
            return Math.Max(0, diff);
        }

        var createdDate = DateOnly.FromDateTime(addiction.CreatedAt.ToUniversalTime());
        var createdDiff = today.DayNumber - createdDate.DayNumber;
        return Math.Max(0, createdDiff);
    }
}
