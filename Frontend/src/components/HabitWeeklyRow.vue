<script setup lang="ts">
import HabitDayCell from './HabitDayCell.vue'
import type { HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useHabitsStore } from '@/stores/habits'
import { toDateOnlyString, getWeekDays, isToday } from '@/utils/dateOnly'
import { getWeekSummaries, type HabitCompletion, type WeekSummary } from '@/utils/habitStreak'

const props = defineProps<{
  habit: HabitWithHistoryDTO
}>()

const habitsStore = useHabitsStore()
const { t, locale } = useI18n()

const expandedWeekKey = ref<string | null>(null)
const pastWeeksScrollRef = ref<HTMLDivElement | null>(null)

function scrollPastWeeksToEnd() {
  nextTick(() => {
    const el = pastWeeksScrollRef.value
    if (el) el.scrollLeft = el.scrollWidth
  })
}

onMounted(() => {
  scrollPastWeeksToEnd()
})

const goal = computed(() => {
  const g = props.habit.habit.timesPerWeekGoal
  const n = typeof g === 'number' ? g : Number(g)
  return Number.isFinite(n) && n >= 1 && n <= 7 ? n : 0
})

function getCompletion(day: Date): HabitCompletion {
  const key = toDateOnlyString(day)
  const entry = props.habit.history.find(h => h.date === key)
  const raw = (entry?.status ?? 'none').toLowerCase()
  return raw === 'full' || raw === 'skip' || raw === 'none' ? raw : 'none'
}

const currentWeekDays = computed(() => getWeekDays(new Date()))

const currentWeekFullCount = computed(() => {
  return currentWeekDays.value.filter(d => getCompletion(d) === 'full').length
})

const currentWeekGoalMet = computed(() => currentWeekFullCount.value >= goal.value)

const pastWeeks = computed(() => getWeekSummaries(props.habit.history, goal.value, 8))

watch(pastWeeks, () => {
  scrollPastWeeksToEnd()
}, { flush: 'post' })

function weekRangeLabel(summary: WeekSummary): string {
  const mon = summary.monday.getDate()
  const sun = new Date(summary.monday)
  sun.setDate(sun.getDate() + 6)
  const sunDate = sun.getDate()
  return `${mon}–${sunDate}`
}

function formatWeekday(date: Date): string {
  return date.toLocaleDateString(locale.value, { weekday: 'short' })
}

function formatMonthLong(date: Date): string {
  return date.toLocaleDateString(locale.value, { month: 'long' })
}

function formatMonthYear(date: Date): string {
  return date.toLocaleDateString(locale.value, { month: 'long', year: 'numeric' })
}

function getMonthKey(date: Date): string {
  const y = date.getFullYear()
  const m = date.getMonth()
  return `${y}-${m}`
}

/** Full month labels for past weeks: one label per month, at the index of the first week of that month. */
const pastWeeksMonthLabels = computed(() => {
  const result: { label: string; startIndex: number }[] = []
  pastWeeks.value.forEach((w, i) => {
    const prev = pastWeeks.value[i - 1]
    const isFirstOfMonth = !prev || getMonthKey(prev.monday) !== getMonthKey(w.monday)
    if (isFirstOfMonth) result.push({ label: formatMonthLong(w.monday), startIndex: i })
  })
  return result
})

/** One label per slot (8 slots); empty string if no month starts at that index. */
const monthLabelBySlot = computed(() => {
  const arr: string[] = []
  for (let i = 0; i < pastWeeks.value.length; i++) {
    const found = pastWeeksMonthLabels.value.find(m => m.startIndex === i)
    arr.push(found ? found.label : '')
  }
  return arr
})

function togglePastWeek(weekKey: string) {
  expandedWeekKey.value = expandedWeekKey.value === weekKey ? null : weekKey
}

const expandedWeekSummary = computed(() => {
  const key = expandedWeekKey.value
  if (!key) return null
  return pastWeeks.value.find(w => w.weekKey === key) ?? null
})

const expandedWeekDays = computed(() => {
  const s = expandedWeekSummary.value
  if (!s) return []
  return getWeekDays(s.monday)
})

function onUpdate(date: Date, value: HabitCompletion) {
  habitsStore.setDayStatus(props.habit.habit.id, date, value)
}
</script>

<template>
  <div class="habit-weekly-row">
    <div class="current-week-block">
      <div class="current-week-header">
        <span class="current-week-label">{{ t('habits.weeklyRow.thisWeek') }}</span>
        <span class="current-week-progress" :class="{ 'goal-met': currentWeekGoalMet }">
          {{ currentWeekFullCount }}/{{ goal }}
        </span>
      </div>
      <div class="weekday-legend weekday-legend-fixed">
        <span
          v-for="day in currentWeekDays"
          :key="toDateOnlyString(day)"
          class="weekday-cell-fixed"
          :class="{ today: isToday(day) }"
        >
          {{ formatWeekday(day) }}
        </span>
      </div>
      <div class="cells-row cells-row-fixed">
        <HabitDayCell
          v-for="day in currentWeekDays"
          :key="toDateOnlyString(day)"
          :date="day"
          :habit="habit"
          :disabled="false"
          strength="100%"
          :streak-number="0"
          @update="onUpdate"
        />
      </div>
    </div>

    <div class="past-weeks-block">
      <div ref="pastWeeksScrollRef" class="past-weeks-scroll">
        <div class="past-weeks-month-row">
          <span
            v-for="(label, i) in monthLabelBySlot"
            :key="'month-slot-' + i"
            class="past-week-month-slot"
          >
            {{ label }}
          </span>
        </div>
        <div class="past-weeks-cells-row">
          <button
            v-for="w in pastWeeks"
            :key="w.weekKey"
            type="button"
            class="past-week-cell"
            :class="{ 'goal-met': w.goalMet, expanded: expandedWeekKey === w.weekKey }"
            :style="habit.habit.color ? { '--habit-color': habit.habit.color } : undefined"
            @click="togglePastWeek(w.weekKey)"
          >
            {{ weekRangeLabel(w) }}
          </button>
        </div>
      </div>
      <div v-if="expandedWeekSummary" class="expanded-week-block">
        <div class="expanded-week-header">
          <span class="expanded-week-label">
            {{ formatMonthYear(expandedWeekSummary.monday) }}
          </span>
          <span class="expanded-week-progress" :class="{ 'goal-met': expandedWeekSummary.goalMet }">
            {{ expandedWeekSummary.count }}/{{ expandedWeekSummary.goal }}
          </span>
        </div>
        <div class="weekday-legend weekday-legend-fixed">
          <span
            v-for="day in expandedWeekDays"
            :key="toDateOnlyString(day)"
            class="weekday-cell-fixed"
          >
            {{ formatWeekday(day) }}
          </span>
        </div>
        <div class="cells-row cells-row-fixed">
          <HabitDayCell
            v-for="day in expandedWeekDays"
            :key="toDateOnlyString(day)"
            :date="day"
            :habit="habit"
            :disabled="false"
            strength="100%"
            :streak-number="0"
            @update="onUpdate"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.habit-weekly-row {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.current-week-block {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.current-week-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 2px;
}

.current-week-label {
  font-size: 0.8rem;
  color: var(--p-text-muted-color);
}

.current-week-progress {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--p-text-color);
}

.current-week-progress.goal-met {
  color: var(--p-green-600);
}

.weekday-legend,
.cells-row {
  display: flex;
}

.weekday-legend-fixed {
  margin-bottom: 2px;
  justify-content: space-evenly;
  width: 100%;
}

.weekday-cell-fixed {
  width: 32px;
  flex-shrink: 0;
  font-size: 0.65rem;
  color: var(--p-text-muted-color);
  text-align: center;
}

.weekday-cell-fixed.today {
  font-weight: 600;
  color: var(--p-primary-color);
}

.cells-row-fixed {
  display: flex;
  justify-content: space-evenly;
  width: 100%;
}

.cells-row-fixed :deep(.cell-wrapper) {
  flex-shrink: 0;
  width: 32px;
  display: flex;
  justify-content: center;
}

.cells-row-fixed :deep(.cell) {
  width: 28px;
  height: 28px;
  flex-shrink: 0;
}

.past-weeks-block {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.past-weeks-scroll {
  overflow-x: auto;
  scrollbar-width: thin;
  margin-left: -2px;
  margin-right: -2px;
  padding-left: 2px;
  padding-right: 2px;
  padding-bottom: 10px;
}

.past-weeks-scroll::-webkit-scrollbar {
  height: 6px;
}

.past-weeks-scroll::-webkit-scrollbar-thumb {
  background: var(--p-content-border-color);
  border-radius: 3px;
}

.past-weeks-month-row,
.past-weeks-cells-row {
  display: flex;
  width: max-content;
  gap: 4px;
}

.past-week-month-slot {
  width: 40px;
  min-width: 40px;
  flex-shrink: 0;
  font-size: 0.7rem;
  color: var(--p-text-muted-color);
  text-align: left;
}

.past-week-cell {
  width: 40px;
  min-width: 40px;
  flex-shrink: 0;
  padding: 6px 2px;
  font-size: 0.65rem;
  font-weight: 500;
  color: var(--p-text-muted-color);
  background-color: var(--p-card-background);
  border: 1px solid var(--p-content-border-color);
  border-radius: 8px;
  cursor: pointer;
  transition: background-color 0.2s, border-color 0.2s;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  text-align: center;
}

.past-week-cell:hover {
  background-color: var(--p-content-hover-background);
  border-color: var(--p-content-hover-border-color);
}

.past-week-cell.goal-met {
  background-color: color-mix(in srgb, var(--habit-color, var(--p-primary-color)) 20%, transparent);
  border-color: var(--habit-color, var(--p-primary-color));
}

.past-week-cell.expanded {
  border-color: var(--p-primary-color);
  box-shadow: 0 0 0 1px var(--p-primary-color);
}

.expanded-week-block {
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px solid var(--p-content-border-color);
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.expanded-week-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 2px;
}

.expanded-week-label {
  font-size: 0.8rem;
  color: var(--p-text-muted-color);
}

.expanded-week-progress {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--p-text-color);
}

.expanded-week-progress.goal-met {
  color: var(--p-green-600);
}
</style>
