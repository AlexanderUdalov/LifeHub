<script setup lang="ts">
import HabitDayCell from './HabitDayCell.vue'
import type { HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { computed, onMounted, ref } from 'vue'
import { rrulestr, RRule } from 'rrule'
import { useHabitsStore } from '@/stores/habits'
import { toDateOnlyString, startOfDay, isToday } from '@/utils/dateOnly'

type HabitCompletion = 'none' | 'skip' | 'full'

const props = defineProps<{
  habit: HabitWithHistoryDTO
}>()

const habitsStore = useHabitsStore()

function getCompletion(day: Date): HabitCompletion {
  const key = toDateOnlyString(day)
  const entry = props.habit.history.find(h => h.date === key)
  const raw = (entry?.status ?? 'none').toLowerCase()
  return raw === 'full' || raw === 'skip' || raw === 'none' ? raw : 'none'
}

const days = computed(() => {
  const result: Date[] = []
  const today = startOfDay(new Date())
  const count = 14
  for (let i = count - 1; i >= 0; i--) {
    const d = new Date(today)
    d.setDate(today.getDate() - i)
    result.push(d)
  }
  return result
})

/** Weekdays from the rule (0=Mon … 6=Sun, RRule convention). Used to avoid timezone shifts from between(). */
const activeWeekdays = computed<Set<number> | null>(() => {
  const ruleStr = props.habit.habit.recurrenceRule?.trim()
  if (!ruleStr) return null

  try {
    const fullStr = ruleStr.toUpperCase().startsWith('RRULE:') ? ruleStr : `RRULE:${ruleStr}`
    const parsed = rrulestr(fullStr, { dtstart: startOfDay(new Date()), unfold: true })

    if (!(parsed instanceof RRule)) return null

    const opt = parsed.options
    const byweekday = opt.byweekday
    if (byweekday == null) return null

    const arr = Array.isArray(byweekday) ? byweekday : [byweekday]
    const nums = arr.map((d: number | { weekday: number }) =>
      typeof d === 'number' ? d : (d as { weekday: number }).weekday
    )
    return new Set(nums)
  } catch {
    return null
  }
})

/** Local weekday in RRule convention: 0=Mon, 1=Tue, … 6=Sun (same as JS getDay() with (d+6)%7). */
function localWeekday(date: Date): number {
  return (date.getDay() + 6) % 7
}

const disabledByIndex = computed(() => {
  const weekdays = activeWeekdays.value
  if (!weekdays) return days.value.map(() => false)
  return days.value.map(d => !weekdays.has(localWeekday(d)))
})

function streakAtIndex(index: number): number {
  let count = 0
  for (let i = index; i >= 0; i--) {
    if (disabledByIndex.value[i]) continue
    const c = getCompletion(days.value[i]!)
    if (c === 'full') {
      count++
      continue
    }
    if (c === 'skip') continue
    break
  }
  return count
}

const streaks = computed(() => days.value.map((_, idx) => streakAtIndex(idx)))

function strengthPercent(streak: number): string {
  if (streak <= 0) return '0%'
  const s = Math.min(streak, 10)
  if (s === 1) return '15%'
  const pct = 30 + Math.round((70 * (s - 1)) / 13)
  return `${pct}%`
}

const strengthByIndex = computed(() => streaks.value.map(s => strengthPercent(s)))

const rowRef = ref<HTMLDivElement | null>(null)
onMounted(() => {
  if (rowRef.value) {
    rowRef.value.scrollLeft = rowRef.value.scrollWidth
  }
})

function onUpdate(date: Date, value: HabitCompletion) {
  habitsStore.setDayStatus(props.habit.habit.id, date, value)
}
</script>

<template>
  <div ref="rowRef" class="day-row">
    <div class="legend-row">
      <div v-for="day in days" :key="toDateOnlyString(day)" class="legend-cell" :class="{ today: isToday(day) }">
        <div class="legend-date">{{ day.getDate() }}</div>
        <div class="legend-weekday" :class="{ today: isToday(day) }">
          {{ day.toLocaleDateString(undefined, { weekday: 'short' }) }}
        </div>
      </div>
    </div>

    <div class="cells-row">
      <HabitDayCell v-for="(day, idx) in days" :key="toDateOnlyString(day)" :date="day" :habit="habit"
        :disabled="disabledByIndex[idx]!" :strength="strengthByIndex[idx]!" :streak-number="streaks[idx]!"
        @update="onUpdate" />
    </div>
  </div>
</template>

<style scoped>
.day-row {
  overflow-x: auto;
  padding-bottom: 4px;
  scrollbar-width: none;
}

.day-row::-webkit-scrollbar {
  display: none;
}

.legend-row,
.cells-row {
  display: flex;
}

.legend-row {
  margin-bottom: 4px;
}

.legend-cell {
  width: 32px;
  display: flex;
  flex-direction: column;
  align-items: center;
  flex-shrink: 0;
}

.legend-cell.today {
  background-color: var(--p-primary-color);
  color: var(--p-primary-contrast-color);
  border-radius: 8px;
}

.legend-date {
  font-size: 0.7rem;
  line-height: 1;
}

.legend-weekday {
  font-size: 0.65rem;
  line-height: 1;
  color: var(--p-text-muted-color);
}

.legend-weekday.today {
  background-color: var(--p-primary-color);
  color: var(--p-primary-contrast-color);
}
</style>
