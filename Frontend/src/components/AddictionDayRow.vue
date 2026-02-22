<script setup lang="ts">
import AddictionDayCell from './AddictionDayCell.vue'
import type { AddictionWithResetsDTO } from '@/api/AddictionsAPI'
import { computed, onMounted, ref } from 'vue'
import { useAddictionsStore } from '@/stores/addictions'
import { toDateOnlyString, startOfDay, isToday } from '@/utils/dateOnly'

const props = defineProps<{
  addiction: AddictionWithResetsDTO
}>()

/** Creation date (start of day) for this addiction; cells before this are disabled. If unknown (no createdAt or very old), null = no cells disabled. */
const createdDate = computed(() => {
  const raw = props.addiction.addiction.createdAt
  if (!raw) return null
  const d = new Date(raw)
  if (d.getFullYear() < 2020) return null
  return startOfDay(d)
})

/** Number of resets on this day (multiple resets per day allowed). */
function resetCountOn(date: Date): number {
  const key = toDateOnlyString(date)
  return props.addiction.resetDates.filter((d) => d === key).length
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

function isDisabled(day: Date): boolean {
  const created = createdDate.value
  if (!created) return false
  const key = toDateOnlyString(day)
  const createdKey = toDateOnlyString(created)
  return key < createdKey
}

/** Streak at index = consecutive days without reset going backwards from that day. Disabled days don't break streak but don't count. */
function streakAtIndex(index: number): number {
  let count = 0
  for (let i = index; i >= 0; i--) {
    const day = days.value[i]!
    if (isDisabled(day)) continue
    if (resetCountOn(day) > 0) break
    count++
  }
  return count
}

const streaks = computed(() => days.value.map((_, idx) => streakAtIndex(idx)))

/** Strength fades with long streak: high when streak 0, low when streak 10+. */
function strengthPercent(streak: number): string {
  if (streak <= 0) return '0%'
  const s = Math.min(streak, 10)
  const pct = 80 - Math.round((65 * s) / 10)
  return `${Math.max(15, pct)}%`
}

const strengthByIndex = computed(() => streaks.value.map((s) => strengthPercent(s)))

const rowRef = ref<HTMLDivElement | null>(null)
onMounted(() => {
  if (rowRef.value) {
    rowRef.value.scrollLeft = rowRef.value.scrollWidth
  }
})

</script>

<template>
  <div ref="rowRef" class="day-row">
    <div class="legend-row">
      <div
        v-for="day in days"
        :key="toDateOnlyString(day)"
        class="legend-cell"
        :class="{ today: isToday(day) }"
      >
        <div class="legend-date">{{ day.getDate() }}</div>
        <div class="legend-weekday" :class="{ today: isToday(day) }">
          {{ day.toLocaleDateString(undefined, { weekday: 'short' }) }}
        </div>
      </div>
    </div>

    <div class="cells-row">
      <AddictionDayCell
        v-for="(day, idx) in days"
        :key="toDateOnlyString(day)"
        :date="day"
        :addiction="addiction"
        :strength="strengthByIndex[idx]!"
        :reset-count="resetCountOn(day)"
        :disabled="isDisabled(day)"
      />
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
