using LifeHub.Models;

namespace LifeHub.DTOs;

public record TaskDTO
(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    DateTimeOffset? CompletionDate,
    string? RecurrenceRule,
    Guid? GoalId
);

public record CreateTaskRequest(
    string Title,
    string? Description,
    DateTimeOffset? DueDate,
    string? RecurrenceRule,
    Guid? GoalId
);

public record UpdateTaskRequest(
    string? Title,
    string? Description,
    DateTimeOffset? DueDate,
    DateTimeOffset? CompletionDate,
    string? RecurrenceRule,
    Guid? GoalId
);

public static class TaskMapping
{
    public static TaskDTO ToDTO(this TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.Description,
            task.DueDate,
            task.CompletionDate,
            task.RecurrenceRule,
            task.GoalId
        );
}