using LifeHub.Models;

namespace LifeHub.DTOs;

public record GoalDTO(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset DueDate,
    Guid? LifeAreaId
);

public record CreateGoalRequest(
    string Title,
    string? Description,
    DateTimeOffset DueDate,
    Guid? LifeAreaId
);

public record UpdateGoalRequest(
    string? Title,
    string? Description,
    DateTimeOffset? DueDate,
    Guid? LifeAreaId
);

public static class GoalMapping
{
    public static GoalDTO ToDTO(this Goal goal) =>
        new(
            goal.Id,
            goal.Title,
            goal.Description,
            new DateTimeOffset(DateTime.SpecifyKind(goal.DueDate, DateTimeKind.Utc)),
            goal.LifeAreaId
        );
}
