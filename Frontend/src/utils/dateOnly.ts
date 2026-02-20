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

