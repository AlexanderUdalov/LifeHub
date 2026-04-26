export function toDateOnlyString(date: Date): string {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

/** Parse YYYY-MM-DD into local Date (no timezone shifting). */
export function fromDateOnlyString(value: string): Date {
  const [y, m, d] = value.split('-').map(Number)
  return new Date(y!, (m ?? 1) - 1, d ?? 1)
}

/**
 * Parse ISO date-time string as UTC. If the string has no timezone (no Z or ±offset),
 * treat it as UTC (backend sends DateTime.UtcNow without Z in some setups).
 */
export function parseUtcIso(isoString: string): Date {
  const s = isoString.trim()
  if (!s) return new Date(0)
  if (s.endsWith('Z') || /[+-]\d{2}:?\d{2}$/.test(s)) return new Date(s)
  return new Date(s + 'Z')
}

/**
 * Encode a local calendar date as an ISO string that is independent of client timezone.
 * Backend still stores DateTimeOffset, but for "date-only" semantics we always use
 * UTC midnight for the selected Y/M/D.
 */
export function toUtcDateOnlyIso(date: Date): string {
  const y = date.getFullYear()
  const m = date.getMonth()
  const d = date.getDate()
  return new Date(Date.UTC(y, m, d, 0, 0, 0, 0)).toISOString()
}

/** Date object for UTC midnight of the given local calendar day. */
export function startOfUtcDay(date: Date): Date {
  const y = date.getFullYear()
  const m = date.getMonth()
  const d = date.getDate()
  return new Date(Date.UTC(y, m, d, 0, 0, 0, 0))
}

export function isSameDateOnly(a: Date, b: Date): boolean {
  return toDateOnlyString(a) === toDateOnlyString(b)
}

export function isToday(date: Date): boolean {
  return isSameDateOnly(date, new Date())
}

export function startOfDay(date: Date): Date {
  const d = new Date(date)
  d.setHours(0, 0, 0, 0)
  return d
}

/** Monday of the week (ISO). Returns YYYY-MM-DD for the Monday of the week containing the given date. */
export function getWeekKey(date: Date): string {
  const d = startOfDay(date)
  const day = d.getDay()
  const mondayOffset = day === 0 ? -6 : 1 - day
  d.setDate(d.getDate() + mondayOffset)
  return toDateOnlyString(d)
}

/** Monday 00:00 of the ISO week containing the given date. */
export function getWeekStart(date: Date): Date {
  return fromDateOnlyString(getWeekKey(date))
}

/** Array of 7 dates (Mon–Sun) for the ISO week containing the given date. */
export function getWeekDays(date: Date): Date[] {
  const monday = getWeekStart(date)
  const result: Date[] = []
  for (let i = 0; i < 7; i++) {
    const d = new Date(monday)
    d.setDate(monday.getDate() + i)
    result.push(d)
  }
  return result
}

/**
 * Detailed elapsed time with months and seconds.
 * Months are approximated as 30-day periods.
 */
export function getTimeSinceDetailed(reference: Date, now: Date = new Date()): {
  months: number
  days: number
  hours: number
  minutes: number
  seconds: number
  totalSeconds: number
} {
  const diffMs = now.getTime() - reference.getTime()
  if (diffMs < 0) return { months: 0, days: 0, hours: 0, minutes: 0, seconds: 0, totalSeconds: 0 }

  const totalSeconds = Math.floor(diffMs / 1000)
  const SEC_IN_MONTH = 30 * 24 * 3600
  const SEC_IN_DAY = 24 * 3600
  const SEC_IN_HOUR = 3600

  let remainder = totalSeconds
  const months = Math.floor(remainder / SEC_IN_MONTH)
  remainder %= SEC_IN_MONTH
  const days = Math.floor(remainder / SEC_IN_DAY)
  remainder %= SEC_IN_DAY
  const hours = Math.floor(remainder / SEC_IN_HOUR)
  remainder %= SEC_IN_HOUR
  const minutes = Math.floor(remainder / 60)
  const seconds = remainder % 60

  return { months, days, hours, minutes, seconds, totalSeconds }
}

