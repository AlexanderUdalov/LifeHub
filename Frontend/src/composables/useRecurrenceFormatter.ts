import { RRule, rrulestr } from 'rrule'
import { useI18n } from 'vue-i18n'

function normalizeWeekday(d: number | { weekday: number }): number {
  return typeof d === 'number' ? d : (d as { weekday: number }).weekday
}

function toWeekdayArray(byweekday: number | number[] | undefined): number[] {
  if (byweekday == null) return []
  return Array.isArray(byweekday) ? byweekday.map(normalizeWeekday) : [normalizeWeekday(byweekday)]
}

function toMonthDayArray(bymonthday: number | number[] | undefined): number[] {
  if (bymonthday == null) return []
  return Array.isArray(bymonthday) ? bymonthday : [bymonthday]
}

/**
 * Returns human-readable recurrence label. Empty string if no recurrence or parse error.
 * Uses task's dueDate as DTSTART when parsing RRULE.
 */
export function useRecurrenceFormatter() {
  const { t } = useI18n()

  function formatRecurrence(
    recurrenceRule: string | undefined | null,
    dueDate: string | undefined | null
  ): string {
    const rruleStr = recurrenceRule?.trim()
    if (!rruleStr) return ''

    try {
      const dtstart = dueDate ? new Date(dueDate) : new Date()
      const fullStr = rruleStr.startsWith('RRULE:') ? rruleStr : `RRULE:${rruleStr}`
      const rule = rrulestr(fullStr, { dtstart, unfold: true })
      if (!(rule instanceof RRule)) return ''

      const opt = rule.options
      const freq = opt.freq
      const interval = opt.interval ?? 1
      const byweekday = toWeekdayArray(opt.byweekday)
      const bymonthday = toMonthDayArray(opt.bymonthday)

      if (freq === RRule.DAILY) {
        if (interval === 1) return t('tasks.recurrence.everyDay')
        return t('tasks.recurrence.everyNDays', { count: interval })
      }

      if (freq === RRule.WEEKLY) {
        if (byweekday.length === 0) {
          return interval === 1 ? t('tasks.recurrence.everyWeek') : t('tasks.recurrence.everyNWeeks', { count: interval })
        }
        const names = byweekday.map((wd) => t(`tasks.recurrence.weekdays.${wd}`))
        const weekdaysStr = names.join(', ')
        if (interval === 1) {
          return names.length === 1
            ? t('tasks.recurrence.weeklyOnWeekday', { weekday: weekdaysStr })
            : t('tasks.recurrence.weeklyOnWeekdays', { weekdays: weekdaysStr })
        }
        return t('tasks.recurrence.everyNWeeksOnWeekdays', { count: interval, weekdays: weekdaysStr })
      }

      if (freq === RRule.MONTHLY) {
        if (bymonthday.length === 0) {
          return interval === 1 ? t('tasks.recurrence.everyMonth') : t('tasks.recurrence.everyNMonths', { count: interval })
        }
        const sorted = [...bymonthday].sort((a, b) => a - b)
        const dayLabels = sorted.map((d) => t('tasks.recurrence.monthDay', { day: d }))
        const daysStr = dayLabels.join(', ')
        return interval === 1
          ? t('tasks.recurrence.monthlyOnDays', { days: daysStr })
          : t('tasks.recurrence.everyNMonthsOnDays', { count: interval, days: daysStr })
      }

      if (freq === RRule.YEARLY) {
        return interval === 1 ? t('tasks.recurrence.everyYear') : t('tasks.recurrence.everyNYears', { count: interval })
      }

      return ''
    } catch {
      return ''
    }
  }

  return { formatRecurrence }
}
