using LifeHub.Models;

namespace LifeHub.DTOs;

public record AddictionDTO(
    Guid Id,
    string Title,
    string Color,
    DateTime CreatedAt,
    Guid? GoalId
);

public record AddictionWithResetsDTO(
    AddictionDTO Addiction,
    IReadOnlyList<DateOnly> ResetDates,
    DateTime? LastResetAt
);

public record AddictionUpsertRequest(
    string Title,
    string Color,
    Guid? GoalId
);

public static class AddictionMapping
{
    public static AddictionDTO ToDTO(this Addiction addiction) =>
        new(
            addiction.Id,
            addiction.Title,
            addiction.Color,
            addiction.CreatedAt,
            addiction.GoalId
        );
}
