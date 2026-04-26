import { RRule, rrulestr } from 'rrule'

export type RecurrencePresetKey = 'none' | 'daily' | 'weekly' | 'monthly' | 'yearly' | 'weekdays'

const WEEKDAYS = [0, 1, 2, 3, 4] as const // MO–FR

/** True if BYDAY appears in the RRULE part (not inferred only by the parser from DTSTART). */
function hasExplicitByDayInRule(rrulePart: string): boolean {
  return /\bBYDAY=/.test(rrulePart.toUpperCase())
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
    const bymonthday = Array.isArray(opt.bymonthday) ? opt.bymonthday : opt.bymonthday != null ? [opt.bymonthday] : []
    const rrulePrefix = /^RRULE:/i.exec(str)
    const rrulePartOnly = rrulePrefix ? str.slice(rrulePrefix[0].length) : str
    const explicitByDay = hasExplicitByDayInRule(rrulePartOnly)

    if (freq === RRule.DAILY && interval === 1) return 'daily'
    if (freq === RRule.WEEKLY && interval === 1) {
      // "Weekly on anchor date" is stored without BYDAY; "pick weekdays" always serializes BYDAY=...
      if (!explicitByDay) return 'weekly'
      return 'weekdays'
    }
    if (freq === RRule.MONTHLY && interval === 1 && bymonthday.length > 0) return 'monthly'
    if (freq === RRule.YEARLY && interval === 1) return 'yearly'
    return 'none'
  } catch {
    return 'none'
  }
}

/** Build RRULE string from preset and date; returns null for 'none'. */
export function presetToRule(preset: RecurrencePresetKey, dt: Date): string | null {
  if (preset === 'none') return null

  const rule = new RRule({
    freq: preset === 'daily' ? RRule.DAILY
      : preset === 'weekly' || preset === 'weekdays' ? RRule.WEEKLY
      : preset === 'monthly' ? RRule.MONTHLY
      : RRule.YEARLY,
    interval: 1,
    // Weekly "same weekday as due date" uses DTSTART only (no BYDAY) so it does not collide with weekday-picker rules
    byweekday: preset === 'weekly' ? undefined : preset === 'weekdays' ? [...WEEKDAYS] : undefined,
    bymonthday: preset === 'monthly' ? [dt.getDate()] : undefined,
    dtstart: dt
  })
  const full = rule.toString()
  const match = full.match(/RRULE:(.+)/)
  const part = match?.[1]
  return part != null ? part.trim() : null
}
