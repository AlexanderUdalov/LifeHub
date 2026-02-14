import { RRule, rrulestr } from 'rrule'

export type RecurrencePresetKey = 'none' | 'daily' | 'weekly' | 'monthly' | 'yearly' | 'weekdays' | 'custom'

const WEEKDAYS = [0, 1, 2, 3, 4] as const // MO–FR

function normalizeWeekday(d: number | { weekday: number }): number {
  return typeof d === 'number' ? d : (d as { weekday: number }).weekday
}

function toWeekdayArray(byweekday: number | number[] | undefined): number[] {
  if (byweekday == null) return []
  return Array.isArray(byweekday) ? byweekday.map(normalizeWeekday) : [normalizeWeekday(byweekday)]
}

/** Detect preset from RRULE string; dtstart used when parsing. */
export function ruleToPreset(
  rruleStr: string | undefined | null,
  dtstart?: Date
): RecurrencePresetKey {
  const str = rruleStr?.trim()
  if (!str) return 'none'

  try {
    const fullStr = str.toUpperCase().startsWith('RRULE:') ? str : `RRULE:${str}`
    const rule = rrulestr(fullStr, { dtstart: dtstart ?? new Date(), unfold: true })
    if (!(rule instanceof RRule)) return 'none'

    const opt = rule.options
    const freq = opt.freq
    const interval = opt.interval ?? 1
    const byweekday = toWeekdayArray(opt.byweekday)
    const bymonthday = Array.isArray(opt.bymonthday) ? opt.bymonthday : opt.bymonthday != null ? [opt.bymonthday] : []

    if (freq === RRule.DAILY && interval === 1) return 'daily'
    if (freq === RRule.WEEKLY && interval === 1) {
      if (byweekday.length === 0) return 'weekly'
      if (byweekday.length === 1) return 'weekly'
      if (byweekday.length === 5) return 'weekdays'
    }
    if (freq === RRule.MONTHLY && interval === 1 && bymonthday.length > 0) return 'monthly'
    if (freq === RRule.YEARLY && interval === 1) return 'yearly'
    return 'custom'
  } catch {
    return 'none'
  }
}

/** Build RRULE string from preset and date; returns null for 'none'. */
export function presetToRule(preset: RecurrencePresetKey, dt: Date): string | null {
  if (preset === 'none' || preset === 'custom') return null

  const rule = new RRule({
    freq: preset === 'daily' ? RRule.DAILY
      : preset === 'weekly' || preset === 'weekdays' ? RRule.WEEKLY
      : preset === 'monthly' ? RRule.MONTHLY
      : RRule.YEARLY,
    interval: 1,
    byweekday: preset === 'weekly' ? [(dt.getDay() + 6) % 7] : preset === 'weekdays' ? [...WEEKDAYS] : undefined,
    bymonthday: preset === 'monthly' ? [dt.getDate()] : undefined,
    dtstart: dt
  })
  const full = rule.toString()
  const match = full.match(/RRULE:(.+)/)
  const part = match?.[1]
  return part != null ? part.trim() : null
}
