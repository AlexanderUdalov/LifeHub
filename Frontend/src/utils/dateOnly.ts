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

export function endOfDay(date: Date): Date {
  const d = startOfDay(date)
  d.setDate(d.getDate() + 1)
  d.setMilliseconds(d.getMilliseconds() - 1)
  return d
}

/**
 * Elapsed time since a reference moment. Pass startOfDay(date) for "time since that calendar day", or exact Date for "time since that moment".
 */
export function getTimeSince(reference: Date, now: Date = new Date()): {
  days: number
  hours: number
  minutes: number
} {
  const diffMs = now.getTime() - reference.getTime()
  if (diffMs < 0) return { days: 0, hours: 0, minutes: 0 }
  const totalMinutes = Math.floor(diffMs / 60_000)
  const days = Math.floor(totalMinutes / (24 * 60))
  const remainderMinutes = totalMinutes % (24 * 60)
  const hours = Math.floor(remainderMinutes / 60)
  const minutes = remainderMinutes % 60
  return { days, hours, minutes }
}

