<script setup lang="ts">
import HabitDayRow from './HabitDayRow.vue'
import HabitWeeklyRow from './HabitWeeklyRow.vue'
import type { HabitDTO, HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { getCurrentDayBasedStreakFallback, getCurrentWeeksStreak } from '@/utils/habitStreak'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import BaseCard from '@/components/base/BaseCard.vue'

const props = withDefaults(
  defineProps<{ habit: HabitWithHistoryDTO; noBorder?: boolean }>(),
  { noBorder: false }
)
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()
const { t, locale } = useI18n()
const areaColor = computed(() => lifeAreasStore.getAreaColorById(props.habit.habit.lifeAreaId))
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

type StreakUnit = 'day' | 'week'

interface StreakInfo {
  value: number
  unit: StreakUnit
}

/** For weekly habits: streak in weeks with fallback to previous week when current one is in progress. */
const streakInfo = computed<StreakInfo | null>(() => {
  if (isWeeklyMode.value && goalNum.value > 0) {
    const weeks = getCurrentWeeksStreak(props.habit.history, goalNum.value)
    return weeks > 0 ? { value: weeks, unit: 'week' } : null
  }
  const raw = props.habit.currentStreak
  const n = typeof raw === 'number' ? raw : Number(raw ?? 0)
  if (Number.isFinite(n) && n > 0) return { value: n, unit: 'day' }

  const fallback = getCurrentDayBasedStreakFallback(props.habit.history)
  return fallback > 0 ? { value: fallback, unit: 'day' } : null
})

function getStreakUnitLabel(unit: StreakUnit, count: number): string {
  const pluralCategory = new Intl.PluralRules(locale.value).select(count)
  const key = `habits.streakUnits.${unit}.${pluralCategory}`
  const translated = t(key)
  if (translated !== key) return translated

  const fallbackKey = `habits.streakUnits.${unit}.other`
  const fallback = t(fallbackKey)
  if (fallback !== fallbackKey) return fallback

  return unit === 'week' ? 'weeks' : 'days'
}

const streakLabel = computed(() => {
  const info = streakInfo.value
  if (!info) return null
  const unit = getStreakUnitLabel(info.unit, info.value)
  return t('habits.currentStreakLabel', { count: info.value, unit })
})

const emit = defineEmits<{
  (e: 'edit', habit: HabitDTO): void
}>()
</script>

<template>
    <BaseCard class="habit-card" :borderless="noBorder" :accent-color="areaColor">
        <template #title>
            <div class="habit-title-block">
                <div class="habit-title" @click="emit('edit', habit.habit)">
                    {{ habit.habit.title }}
                </div>
            </div>
        </template>

        <template #content>
            <div v-if="goalTitle || (streakInfo && streakLabel)" class="goal-streak-row">
                <div v-if="goalTitle" class="goal-text">
                    {{ goalTitle }}
                </div>
                <span v-if="streakInfo && streakLabel" class="habit-streak-chip">
                    {{ streakLabel }}
                </span>
            </div>
            <HabitWeeklyRow v-if="isWeeklyMode" :habit="habit" />
            <HabitDayRow v-else :habit="habit" />
        </template>
    </BaseCard>
</template>

<style scoped>
.habit-card {
  border-radius: var(--ds-radius-lg);
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

.goal-streak-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
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
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1 1 auto;
  min-width: 0;
}
</style>
