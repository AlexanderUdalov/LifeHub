import type { HabitDTO, HabitWithHistoryDTO, HabitCompletion } from '@/api/HabitsAPI'
import { fromDateOnlyString, getWeekKey, toDateOnlyString } from './dateOnly'

export function scheduleOn(habit: HabitDTO, date: string) {
  const schedule = habit.schedules
    .filter((s) => s.effectiveFrom <= date)
    .sort((a, b) => b.effectiveFrom.localeCompare(a.effectiveFrom))[0]
  const rule = schedule?.recurrenceRule ?? habit.recurrenceRule
  const goal = Number(schedule ? schedule.timesPerWeekGoal : habit.timesPerWeekGoal) || null
  return { rule, goal }
}

export function pendingSchedule(habit: HabitDTO, today: string) {
  return habit.schedules
    .filter((s) => s.effectiveFrom > today)
    .sort((a, b) => a.effectiveFrom.localeCompare(b.effectiveFrom))[0]
}

export function isHabitActiveOn(habit: HabitDTO, date: string) {
  return (
    date >= habit.startDate &&
    !habit.inactivePeriods.some((p) => p.startDate <= date && (!p.endDate || date < p.endDate))
  )
}

export function isHabitScheduled(habit: HabitDTO, date: string) {
  if (!isHabitActiveOn(habit, date)) return false
  const { rule, goal } = scheduleOn(habit, date)
  if (goal) return true
  const days = rule
    .toUpperCase()
    .match(/(?:^|;)BYDAY=([^;]+)/)?.[1]
    ?.split(',')
  const code = ['SU', 'MO', 'TU', 'WE', 'TH', 'FR', 'SA'][fromDateOnlyString(date).getDay()]!
  return !days || days.includes(code)
}

export function statusOn(item: HabitWithHistoryDTO, date: string): HabitCompletion {
  const status = item.history.find((d) => d.date === date)?.status
  return status === 'full' || status === 'skip' ? status : 'none'
}

export function canMarkDay(item: HabitWithHistoryDTO, date: string, today: string) {
  return date <= today && (isHabitScheduled(item.habit, date) || statusOn(item, date) !== 'none')
}

export function weekCompletions(item: HabitWithHistoryDTO, date: string) {
  const monday = getWeekKey(fromDateOnlyString(date))
  const end = fromDateOnlyString(monday)
  end.setDate(end.getDate() + 6)
  const sunday = toDateOnlyString(end)
  return item.history.filter((d) => d.date >= monday && d.date <= sunday && d.status === 'full')
    .length
}

export function dateRange(from: string, to: string): string[] {
  const days: string[] = []
  for (const d = fromDateOnlyString(from); toDateOnlyString(d) <= to; d.setDate(d.getDate() + 1)) {
    days.push(toDateOnlyString(d))
  }
  return days
}

export function planStats(item: HabitWithHistoryDTO, from: string, to: string, today: string) {
  let planned = 0,
    done = 0,
    skipped = 0
  for (const day of dateRange(from, to)) {
    if (day >= today || !isHabitScheduled(item.habit, day)) continue
    const status = statusOn(item, day)
    if (status === 'skip') skipped++
    else if (!scheduleOn(item.habit, day).goal) {
      planned++
      if (status === 'full') done++
    }
  }
  return { planned, done, skipped }
}

export function scheduleWeekdays(rule: string): number[] {
  const codes = rule
    .toUpperCase()
    .match(/(?:^|;)BYDAY=([^;]+)/)?.[1]
    ?.split(',')
  return codes
    ? ['MO', 'TU', 'WE', 'TH', 'FR', 'SA', 'SU'].flatMap((code, i) =>
        codes.includes(code) ? [i] : [],
      )
    : [0, 1, 2, 3, 4, 5, 6]
}
