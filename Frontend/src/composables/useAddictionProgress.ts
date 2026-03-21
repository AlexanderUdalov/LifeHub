import { computed, type Ref } from 'vue'
import type { AddictionWithResetsDTO } from '@/api/AddictionsAPI'
import { getTimeSinceDetailed, parseUtcIso } from '@/utils/dateOnly'

export interface Milestone {
  seconds: number
  labelKey: string
}

export const MILESTONES: Milestone[] = [
  { seconds: 3_600, labelKey: 'addictions.milestones.1hour' },
  { seconds: 86_400, labelKey: 'addictions.milestones.1day' },
  { seconds: 259_200, labelKey: 'addictions.milestones.3days' },
  { seconds: 604_800, labelKey: 'addictions.milestones.1week' },
  { seconds: 864_000, labelKey: 'addictions.milestones.10days' },
  { seconds: 1_209_600, labelKey: 'addictions.milestones.2weeks' },
  { seconds: 2_592_000, labelKey: 'addictions.milestones.1month' },
  { seconds: 5_184_000, labelKey: 'addictions.milestones.2months' },
  { seconds: 7_776_000, labelKey: 'addictions.milestones.3months' },
  { seconds: 15_552_000, labelKey: 'addictions.milestones.6months' },
  { seconds: 31_536_000, labelKey: 'addictions.milestones.1year' },
]

export function useAddictionProgress(
  addiction: Ref<AddictionWithResetsDTO>,
  now: Ref<Date>
) {
  const referenceDate = computed(() => {
    const lastResetAt = addiction.value.lastResetAt
    if (lastResetAt) return parseUtcIso(lastResetAt)

    const createdAt = addiction.value.addiction.createdAt
    if (createdAt) return new Date(createdAt)

    return now.value
  })

  const elapsed = computed(() => getTimeSinceDetailed(referenceDate.value, now.value))

  const currentMilestoneIndex = computed(() => {
    const total = elapsed.value.totalSeconds
    return MILESTONES.findIndex((m) => total < m.seconds)
  })

  const nextMilestone = computed((): Milestone | null => {
    const idx = currentMilestoneIndex.value
    if (idx === -1) return null
    return MILESTONES[idx]!
  })

  const prevMilestoneSeconds = computed(() => {
    const idx = currentMilestoneIndex.value
    if (idx <= 0) return 0
    return MILESTONES[idx - 1]!.seconds
  })

  const progressPercent = computed(() => {
    const next = nextMilestone.value
    if (!next) return 100
    const prev = prevMilestoneSeconds.value
    const range = next.seconds - prev
    if (range <= 0) return 100
    const current = elapsed.value.totalSeconds - prev
    return Math.min(100, Math.max(0, Math.floor((current / range) * 100)))
  })

  return {
    referenceDate,
    elapsed,
    nextMilestone,
    progressPercent,
  }
}
