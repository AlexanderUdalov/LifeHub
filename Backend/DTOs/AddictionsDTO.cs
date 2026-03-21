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

public record AddictionResetEntryDTO(
    Guid Id,
    DateOnly Date,
    DateTime ResetAt,
    Guid? JournalEntryId,
    string? JournalText
);

public record AddictionWithResetsDTO(
    AddictionDTO Addiction,
    IReadOnlyList<AddictionResetEntryDTO> Resets,
    DateTime? LastResetAt,
    int CurrentStreakDays
);

public record AddictionUpsertRequest(
    string Title,
    string Color,
    Guid? GoalId,
    Guid? LifeAreaId,
    /// <summary>Optional. When creating, adds one reset at this moment (UTC). Ignored on update.</summary>
    DateTime? LastRelapseAt
);

public record SetResetRequest(
    /// <summary>Optional. When set, a <see cref="JournalEntry"/> is created and linked to this reset.</summary>
    string? Note,
    /// <summary>Optional. When set, used as <see cref="AddictionReset.ResetAt"/>; calendar <see cref="AddictionReset.Date"/> is derived from this instant in UTC.</summary>
    DateTime? ResetAt
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
