import { computed, unref, type MaybeRef, type Ref } from 'vue'
import type { AddictionWithResetsDTO } from '@/api/AddictionsAPI'
import { getTimeSinceDetailed, parseUtcIso } from '@/utils/dateOnly'

interface Milestone {
  seconds: number
  labelKey: string
}

const MILESTONES: Milestone[] = [
  { seconds: 3_600, labelKey: 'addictions.stages.1hour' },
  { seconds: 86_400, labelKey: 'addictions.stages.1day' },
  { seconds: 259_200, labelKey: 'addictions.stages.3days' },
  { seconds: 604_800, labelKey: 'addictions.stages.1week' },
  { seconds: 864_000, labelKey: 'addictions.stages.10days' },
  { seconds: 1_209_600, labelKey: 'addictions.stages.2weeks' },
  { seconds: 2_592_000, labelKey: 'addictions.stages.1month' },
  { seconds: 5_184_000, labelKey: 'addictions.stages.2months' },
  { seconds: 7_776_000, labelKey: 'addictions.stages.3months' },
  { seconds: 15_552_000, labelKey: 'addictions.stages.6months' },
  { seconds: 31_536_000, labelKey: 'addictions.stages.1year' },
]

export function useAddictionProgress(
  addiction: MaybeRef<AddictionWithResetsDTO>,
  now: Ref<Date>
) {
  const referenceDate = computed(() => {
    const a = unref(addiction)
    const lastResetAt = a.lastResetAt
    if (lastResetAt) return parseUtcIso(lastResetAt)

    const createdAt = a.addiction.createdAt
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
