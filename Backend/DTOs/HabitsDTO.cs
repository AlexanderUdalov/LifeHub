using LifeHub.Models;
using LifeHub.Services;

namespace LifeHub.DTOs;

public record HabitScheduleDTO(DateOnly EffectiveFrom, string RecurrenceRule, int? TimesPerWeekGoal);
public record HabitInactivePeriodDTO(DateOnly StartDate, DateOnly? EndDate);
public record HabitDTO(
    Guid Id, string Title, string Color, string RecurrenceRule, int? TimesPerWeekGoal,
    Guid? GoalId, Guid? LifeAreaId, DateOnly StartDate, string State,
    IReadOnlyList<HabitScheduleDTO> Schedules, IReadOnlyList<HabitInactivePeriodDTO> InactivePeriods
);
public record HabitDayDTO(DateOnly Date, string Status);
public record HabitWithHistoryDTO(HabitDTO Habit, IReadOnlyList<HabitDayDTO> History, int CurrentStreak);
public record HabitUpsertRequest(
    string Title, string Color, string RecurrenceRule, int? TimesPerWeekGoal, Guid? GoalId, Guid? LifeAreaId
);
public record SetDayStatusRequest(string Status);
public record SetHabitStateRequest(string State);
public record HabitDayUpdateDTO(DateOnly Date, string Status, int CurrentStreak);
public record HabitStateUpdateDTO(HabitDTO Habit, int CurrentStreak);
public record HabitHistoryDTO(IReadOnlyList<HabitDayDTO> Items, DateOnly From, DateOnly To, bool HasEarlier);

public static class HabitMapping
{
    public static HabitDTO ToDTO(this Habit habit, DateOnly? date = null)
    {
        var day = date ?? DateOnly.FromDateTime(DateTime.Today);
        var schedule = HabitProgress.ScheduleOn(habit, day);
        return new(habit.Id, habit.Title, habit.Color, schedule.Rule, schedule.Goal,
            habit.GoalId, habit.LifeAreaId, habit.StartDate, habit.State.ToString().ToLowerInvariant(),
            habit.Schedules.OrderBy(x => x.EffectiveFrom)
                .Select(x => new HabitScheduleDTO(x.EffectiveFrom, x.RecurrenceRule, x.TimesPerWeekGoal)).ToList(),
            habit.InactivePeriods.OrderBy(x => x.StartDate)
                .Select(x => new HabitInactivePeriodDTO(x.StartDate, x.EndDate)).ToList());
    }
    public static HabitDayDTO ToDTO(this HabitDay day) => new(day.Date, day.Status.ToString().ToLowerInvariant());
}
