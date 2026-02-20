using LifeHub.Models;

namespace LifeHub.DTOs;

public record HabitDTO(
    Guid Id,
    string Title,
    string Color,
    string RecurrenceRule,
    Guid? GoalId
);

public record HabitDayDTO(
    DateOnly Date,
    string Status
);

public record HabitWithHistoryDTO(
    HabitDTO Habit,
    IReadOnlyList<HabitDayDTO> History
);

public record HabitUpsertRequest(
    string Title,
    string Color,
    string RecurrenceRule,
    Guid? GoalId
);

public record SetDayStatusRequest(string Status);

public static class HabitMapping
{
    public static HabitDTO ToDTO(this Habit habit) =>
        new(
            habit.Id,
            habit.Title,
            habit.Color,
            habit.RecurrenceRule,
            habit.GoalId
        );

    public static HabitDayDTO ToDTO(this HabitDay day) =>
        new(day.Date, day.Status.ToString().ToLowerInvariant());
}

