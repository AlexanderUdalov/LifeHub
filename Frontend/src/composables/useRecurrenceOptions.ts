import { RRule, rrulestr, type Options } from 'rrule'

/** State for recurrence UI: freq, interval, byweekday (normalized to number[]), bymonthday. */
export type RecurrenceOptionsState = Partial<Pick<Options, 'interval'>> & {
  freq: Options['freq'] | null
  byweekday?: number[]
  bymonthday?: number[]
}

const DEFAULT_STATE: RecurrenceOptionsState = {
  freq: null,
  interval: 1,
  byweekday: [],
  bymonthday: []
}

function normalizeWeekday(d: number | { weekday: number }): number {
  return typeof d === 'number' ? d : (d as { weekday: number }).weekday
}

function toWeekdayArray(byweekday: number | number[] | undefined): number[] {
  if (byweekday == null) return []
  return Array.isArray(byweekday) ? byweekday.map(normalizeWeekday) : [normalizeWeekday(byweekday)]
}

function toMonthDayArray(bymonthday: number | number[] | undefined): number[] {
  if (bymonthday == null) return []
  return Array.isArray(bymonthday) ? [...bymonthday].sort((a, b) => a - b) : [bymonthday]
}

/**
 * Parse RRULE string into options state for UI (freq, interval, byweekday, bymonthday).
 * Uses dtstart when RRULE has no DTSTART.
 */
/** Parse RRULE string into options state; uses dtstart when RRULE has no DTSTART. */
export function parseRuleToOptions(
  rruleStr: string | undefined | null,
  dtstart?: Date
): RecurrenceOptionsState {
  const str = rruleStr?.trim()
  if (!str) return { ...DEFAULT_STATE }

  try {
    const fullStr = str.toUpperCase().startsWith('RRULE:') ? str : `RRULE:${str}`
    const rule = rrulestr(fullStr, { dtstart: dtstart ?? new Date(), unfold: true })
    if (!(rule instanceof RRule)) return { ...DEFAULT_STATE }

    const opt = rule.options
    return {
      freq: opt.freq,
      interval: opt.interval ?? 1,
      byweekday: toWeekdayArray(opt.byweekday),
      bymonthday: toMonthDayArray(opt.bymonthday)
    }
  } catch {
    return { ...DEFAULT_STATE }
  }
}

/**
 * Build RRULE string (value only, no "RRULE:" prefix) from options state.
 * Returns null when freq is null (no recurrence).
 */
export function optionsToRuleString(state: RecurrenceOptionsState, dtstart?: Date): string | null {
  if (state.freq == null) return null

  const start = dtstart ?? new Date()
  const byweekdayArr = state.byweekday ?? []
  const bymonthdayArr = state.bymonthday ?? []
  const byweekday = byweekdayArr.length ? byweekdayArr : undefined
  const bymonthday = bymonthdayArr.length ? bymonthdayArr : undefined
  const rule = new RRule({
    freq: state.freq,
    interval: Math.max(1, state.interval ?? 1),
    byweekday,
    bymonthday,
    dtstart: start
  })
  const full = rule.toString()
  const match = full.match(/RRULE:(.+)/)
  const part = match?.[1]
  return part != null ? part.trim() : null
}
