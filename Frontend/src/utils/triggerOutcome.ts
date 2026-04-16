/** Backend serializes enums as camelCase strings (JsonStringEnumConverter). */
export function isTriggerOvercameOutcome(outcome: unknown): boolean {
  if (outcome === 0 || outcome === '0') return true
  if (typeof outcome === 'string') {
    const s = outcome.trim().toLowerCase()
    return s === 'overcame'
  }
  return false
}
