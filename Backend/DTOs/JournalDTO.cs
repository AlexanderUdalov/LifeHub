using LifeHub.Models;

namespace LifeHub.DTOs;

public record JournalEntryDTO(
    Guid Id,
    string Text,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsPinned,
    DateTimeOffset? PinnedAt,
    Guid? TaskItemId,
    Guid? HabitId,
    Guid? AddictionId,
    Guid? GoalId,
    Guid? LifeAreaId
);

public record CreateJournalEntryRequest(
    string Text,
    Guid? TaskItemId,
    Guid? HabitId,
    Guid? AddictionId,
    Guid? GoalId,
    Guid? LifeAreaId
);

public record UpdateJournalEntryRequest(
    string? Text,
    DateTimeOffset? CreatedAt,
    bool? IsPinned,
    Guid? TaskItemId,
    Guid? HabitId,
    Guid? AddictionId,
    Guid? GoalId,
    Guid? LifeAreaId
);

public static class JournalEntryMapping
{
    public static JournalEntryDTO ToDTO(this JournalEntry entry) =>
        new(
            entry.Id,
            entry.Text,
            entry.CreatedAt,
            entry.UpdatedAt,
            entry.IsPinned,
            entry.PinnedAt,
            entry.TaskItemId,
            entry.HabitId,
            entry.AddictionId,
            entry.GoalId,
            entry.LifeAreaId
        );
}

