import { computed, type Ref } from 'vue'
import type { AddictionItem } from '@/models/AddictionItem'

const STAGES_HOURS = [24, 48, 72, 168, 240, 720]

export function useAddictionProgress(addiction: Ref<AddictionItem>) {
    const lastResetDate = computed(() => new Date(addiction.value.lastReset))

    const elapsedMs = computed(() => Date.now() - lastResetDate.value.getTime())
    const elapsedHours = computed(() =>
        Math.floor(elapsedMs.value / 1000 / 60 / 60)
    )

    const elapsedText = computed(() => {
        const hours = elapsedHours.value
        const days = Math.floor(hours / 24)
        const remainingHours = hours % 24

        if (days > 0) {
            return `${days} day${days > 1 ? 's' : ''} ${remainingHours} hour${remainingHours !== 1 ? 's' : ''} clean`
        }

        return `${hours} hour${hours !== 1 ? 's' : ''} clean`
    })

    const currentStage = computed(() =>
        STAGES_HOURS.find(stage => elapsedHours.value < stage)
    )

    const previousStage = computed(() => {
        const index = STAGES_HOURS.indexOf(currentStage.value ?? 24)
        return index > 0 ? STAGES_HOURS[index - 1] : 0
    })

    const progressPercent = computed(() => {
        if (!currentStage.value || !previousStage.value) return 100
        const stageRange = currentStage.value - previousStage.value
        const progressInStage = elapsedHours.value - previousStage.value
        return Math.min(100, Math.floor((progressInStage / stageRange) * 100))
    })

    const nextStageText = computed(() => {
        if (!currentStage.value) return 'Maximum stage reached'

        const remainingHours = currentStage.value - elapsedHours.value
        if (remainingHours >= 24) {
            const days = Math.ceil(remainingHours / 24)
            return `Next stage in ${days} day${days > 1 ? 's' : ''}`
        }
        return `Next stage in ${remainingHours} hour${remainingHours !== 1 ? 's' : ''}`
    })

    return {
        elapsedHours,
        elapsedText,
        progressPercent,
        currentStage,
        nextStageText
    }
}
