<script setup lang="ts">
import { computed } from 'vue'
import type { AddictionItem } from '@/models/AddictionItem';
import Card from 'primevue/card';
import ProgressBar from 'primevue/progressbar'

const STAGES_HOURS = [
    24,
    48,
    72,
    168,
    240,
    720
]

const props = defineProps<{ addiction: AddictionItem }>()
const lastResetDate = computed(() => new Date(props.addiction.lastReset))

const elapsedMs = computed(() => Date.now() - lastResetDate.value.getTime())
const elapsedHours = computed(() => Math.floor(elapsedMs.value / 1000 / 60 / 60))
const elapsedText = computed(() => {
    const hours = elapsedHours.value

    const days = Math.floor(hours / 24)
    const remainingHours = hours % 24

    if (days > 0) {
        return `${days} day${days > 1 ? 's' : ''} ${remainingHours} hour${remainingHours !== 1 ? 's' : ''} clean`
    }

    return `${hours} hour${hours !== 1 ? 's' : ''} clean`
})

const currentStage = computed(() => {
  return STAGES_HOURS.find(stage => elapsedHours.value < stage)
})

const previousStage = computed(() => {
  const index = STAGES_HOURS.indexOf(currentStage.value ?? STAGES_HOURS[STAGES_HOURS.length - 1])
  return index > 0 ? STAGES_HOURS[index - 1] : 0
})

const progressPercent = computed(() => {
  if (!currentStage.value) return 100

  const stageRange = currentStage.value - previousStage.value
  const progressInStage = elapsedHours.value - previousStage.value

  return Math.min(100, Math.floor((progressInStage / stageRange) * 100))
})

const nextStageText = computed(() => {
  if (!currentStage.value) {
    return 'Maximum stage reached'
  }

  const remainingHours = currentStage.value - elapsedHours.value

  if (remainingHours >= 24) {
    const days = Math.ceil(remainingHours / 24)
    return `Next stage in ${days} day${days > 1 ? 's' : ''}`
  }

  return `Next stage in ${remainingHours} hour${remainingHours !== 1 ? 's' : ''}`
})


</script>

<template>
    <Card class="addiction-card">
        <template #content>
            <div class="card-header">
                <div class="icon-wrapper">
                    <i class="pi icon" :class="addiction.icon" />
                </div>
                <div class="text-wrapper">
                    <div class="title">
                        {{ addiction.title }}
                    </div>
                    <div class="elapsed">
                        {{ elapsedText }}
                    </div>
                </div>
            </div>

            <div class="progress-wrapper">
                <ProgressBar :value="progressPercent">{{ nextStageText }}</ProgressBar>
            </div>
        </template>
    </Card>
</template>


<style>
.addiction-card {
    margin: 12px;
}

.card-header {
    display: flex;
    gap: 12px;
    align-items: center;
}

.icon-wrapper {
    width: 40px;
    height: 40px;
    border-radius: 12px;
    background: var(--surface-200);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}

.icon {
    font-size: 20px;
}

.text-wrapper {
    display: flex;
    flex-direction: column;
}

.title {
    font-weight: 600;
    line-height: 1.2;
}

.elapsed {
    font-size: 0.85rem;
    color: var(--text-color-secondary);
}

.progress-wrapper {
    margin-top: 12px;
}
</style>