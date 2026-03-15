<script setup lang="ts">
import Card from 'primevue/card'
import HabitDayRow from './HabitDayRow.vue'
import HabitWeeklyRow from './HabitWeeklyRow.vue'
import type { HabitDTO, HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { getCurrentWeeksStreak } from '@/utils/habitStreak'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'

const props = withDefaults(
  defineProps<{ habit: HabitWithHistoryDTO; noBorder?: boolean }>(),
  { noBorder: false }
)
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()
const { t } = useI18n()
const areaColor = computed(() => lifeAreasStore.getAreaColorById(props.habit.habit.lifeAreaId))
const cardBorderStyle = computed(() => {
  if (props.noBorder) return { border: 'none', borderLeftWidth: 0 }
  return areaColor.value ? { borderLeftWidth: '4px', borderLeftStyle: 'solid', borderLeftColor: areaColor.value } : { borderLeftWidth: 0 }
})
const goalTitle = computed(() => goalsStore.getGoalById(props.habit.habit.goalId)?.title ?? null)
const isWeeklyMode = computed(() => {
  const g = props.habit.habit.timesPerWeekGoal
  const n = typeof g === 'number' ? g : Number(g)
  return Number.isFinite(n) && n >= 1 && n <= 7
})

const goalNum = computed(() => {
  const g = props.habit.habit.timesPerWeekGoal
  const n = typeof g === 'number' ? g : Number(g)
  return Number.isFinite(n) && n >= 1 && n <= 7 ? n : 0
})

/** For weekly habits: streak in weeks (computed from history, shown until Monday reset). Otherwise backend value. */
const streakValue = computed(() => {
  if (isWeeklyMode.value && goalNum.value > 0) {
    const weeks = getCurrentWeeksStreak(props.habit.history, goalNum.value)
    return weeks > 0 ? weeks : null
  }
  const raw = props.habit.currentStreak
  const n = typeof raw === 'number' ? raw : Number(raw ?? 0)
  if (!Number.isFinite(n) || n <= 0) return null
  return n
})

const emit = defineEmits<{
  (e: 'edit', habit: HabitDTO): void
}>()
</script>

<template>
    <Card class="habit-card" :class="{ 'no-border': noBorder }" :style="cardBorderStyle">
        <template #title>
            <div class="habit-title-block">
                <div class="habit-title" @click="emit('edit', habit.habit)">
                    {{ habit.habit.title }}
                </div>
                <div v-if="streakValue" class="habit-streak-row">
                    <span class="habit-streak-chip">
                        {{ t('habits.currentStreakLabel', { count: streakValue }) }}
                    </span>
                </div>
            </div>
        </template>

        <template #content>
            <div v-if="goalTitle" class="goal-text">
                {{ goalTitle }}
            </div>
            <HabitWeeklyRow v-if="isWeeklyMode" :habit="habit" />
            <HabitDayRow v-else :habit="habit" />
        </template>
    </Card>
</template>

<style scoped>
.habit-card {
  border-radius: 16px;
  border-left-width: 4px;
  border-left-style: solid;
}

.habit-card.no-border {
  border: none;
  border-left-width: 0;
}

.habit-card.no-border :deep(.p-card) {
  border: none;
  box-shadow: none;
}

.habit-title-block {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.habit-title {
  cursor: pointer;
  user-select: none;
}

.habit-streak-row {
  display: flex;
  align-items: center;
}

.habit-streak-chip {
  padding: 0.1rem 0.5rem;
  border-radius: 999px;
  background-color: var(--p-primary-100);
  color: var(--p-primary-700);
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
}

.goal-text {
  color: var(--p-text-muted-color);
  font-size: 0.875rem;
  margin-bottom: 0.5rem;
}
</style>
