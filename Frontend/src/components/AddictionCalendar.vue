<script setup lang="ts">
import { computed, ref } from 'vue'
import Button from 'primevue/button'
import { useI18n } from 'vue-i18n'
import type { AddictionResetEntryDTO } from '@/api/AddictionsAPI'
import { toDateOnlyString, isToday, startOfDay } from '@/utils/dateOnly'

const props = defineProps<{
  resets: AddictionResetEntryDTO[]
  createdAt: string
  color: string
}>()

const emit = defineEmits<{
  (e: 'cell-click', date: Date, hasReset: boolean): void
}>()

const { locale, t } = useI18n()

const intlLocale = computed(() => (locale.value === 'ru' ? 'ru-RU' : 'en-US'))

const viewMonth = ref(new Date().getMonth())
const viewYear = ref(new Date().getFullYear())

const weekdayLabels = computed(() => {
  const base = new Date(2024, 0, 1)
  const labels: string[] = []
  for (let i = 0; i < 7; i++) {
    const d = new Date(base)
    d.setDate(base.getDate() + i)
    labels.push(d.toLocaleDateString(intlLocale.value, { weekday: 'short' }))
  }
  return labels
})

const monthLabel = computed(() => {
  const date = new Date(viewYear.value, viewMonth.value, 1)
  return date.toLocaleDateString(intlLocale.value, { month: 'long', year: 'numeric' })
})

const canGoNext = computed(() => {
  const now = new Date()
  return (
    viewYear.value < now.getFullYear() ||
    (viewYear.value === now.getFullYear() && viewMonth.value < now.getMonth())
  )
})

function prevMonth() {
  if (viewMonth.value === 0) {
    viewMonth.value = 11
    viewYear.value--
  } else {
    viewMonth.value--
  }
}

function nextMonth() {
  if (!canGoNext.value) return
  if (viewMonth.value === 11) {
    viewMonth.value = 0
    viewYear.value++
  } else {
    viewMonth.value++
  }
}

interface CalendarCell {
  date: Date
  dateKey: string
  dayNumber: number
  isCurrentMonth: boolean
  isToday: boolean
  isFuture: boolean
  isBeforeCreation: boolean
  resetCount: number
}

const resetDateSet = computed(() => {
  const map = new Map<string, number>()
  for (const r of props.resets ?? []) {
    map.set(r.date, (map.get(r.date) ?? 0) + 1)
  }
  return map
})

/** Compare calendar days using local YYYY-MM-DD only (avoids UTC/local drift from createdAt). */
function isDayBeforeCreation(d: Date): boolean {
  if (!props.createdAt) return false
  const createdKey = toDateOnlyString(new Date(props.createdAt))
  return toDateOnlyString(d) < createdKey
}

const calendarGrid = computed((): CalendarCell[] => {
  const year = viewYear.value
  const month = viewMonth.value
  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)
  const today = startOfDay(new Date())
  const resetMap = resetDateSet.value

  let startOffset = firstDay.getDay() - 1
  if (startOffset < 0) startOffset = 6

  const cells: CalendarCell[] = []

  for (let i = startOffset - 1; i >= 0; i--) {
    const d = new Date(year, month, -i)
    const key = toDateOnlyString(d)
    cells.push({
      date: d,
      dateKey: key,
      dayNumber: d.getDate(),
      isCurrentMonth: false,
      isToday: false,
      isFuture: d > today,
      isBeforeCreation: isDayBeforeCreation(d),
      resetCount: resetMap.get(key) ?? 0
    })
  }

  for (let day = 1; day <= lastDay.getDate(); day++) {
    const d = new Date(year, month, day)
    const key = toDateOnlyString(d)
    cells.push({
      date: d,
      dateKey: key,
      dayNumber: day,
      isCurrentMonth: true,
      isToday: isToday(d),
      isFuture: d > today,
      isBeforeCreation: isDayBeforeCreation(d),
      resetCount: resetMap.get(key) ?? 0
    })
  }

  while (cells.length % 7 !== 0) {
    const idx = cells.length - startOffset - lastDay.getDate()
    const d = new Date(year, month + 1, idx + 1)
    const key = toDateOnlyString(d)
    cells.push({
      date: d,
      dateKey: key,
      dayNumber: d.getDate(),
      isCurrentMonth: false,
      isToday: false,
      isFuture: d > today,
      isBeforeCreation: isDayBeforeCreation(d),
      resetCount: resetMap.get(key) ?? 0
    })
  }

  return cells
})

function isInteractive(cell: CalendarCell): boolean {
  return !cell.isFuture && !cell.isBeforeCreation
}

function onCellClick(cell: CalendarCell) {
  if (!isInteractive(cell)) return
  emit('cell-click', cell.date, cell.resetCount > 0)
}
</script>

<template>
  <div class="addiction-calendar" :style="{ '--calendar-accent': color }">
    <div class="cal-header">
      <Button icon="pi pi-chevron-left" text rounded size="small" @click="prevMonth" />
      <span class="cal-month-label">{{ monthLabel }}</span>
      <Button icon="pi pi-chevron-right" text rounded size="small" :disabled="!canGoNext" @click="nextMonth" />
    </div>

    <div class="cal-grid">
      <div v-for="wd in weekdayLabels" :key="wd" class="cal-weekday">{{ wd }}</div>

      <div
        v-for="(cell, cIdx) in calendarGrid"
        :key="'cal-' + cIdx + '-' + cell.dateKey"
        class="cal-cell"
        :class="{
          'other-month': !cell.isCurrentMonth,
          'is-today': cell.isToday,
          'has-reset': cell.resetCount > 0,
          disabled: !isInteractive(cell),
          interactive: isInteractive(cell)
        }"
        @click="onCellClick(cell)"
      >
        <span class="cal-day">{{ cell.dayNumber }}</span>
        <span v-if="cell.resetCount > 0" class="cal-reset-dot" />
      </div>
    </div>

    <div class="cal-legend">
      <div class="cal-legend-item">
        <span class="cal-legend-swatch clean" />
        <span>{{ t('addictions.calendarLegend.cleanDay') }}</span>
      </div>
      <div class="cal-legend-item">
        <span class="cal-legend-swatch reset" />
        <span>{{ t('addictions.calendarLegend.resetDay') }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.addiction-calendar {
  width: 100%;
}

.cal-header {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  margin-bottom: 8px;
}

.cal-month-label {
  font-weight: 600;
  font-size: 0.95rem;
  min-width: 140px;
  text-align: center;
}

.cal-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 2px;
}

.cal-weekday {
  text-align: center;
  font-size: 0.7rem;
  color: var(--p-text-muted-color);
  padding: 4px 0;
  font-weight: 500;
}

.cal-cell {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  aspect-ratio: 1;
  border-radius: 8px;
  border: 1px solid transparent;
  font-size: 0.8rem;
  transition:
    background-color 0.15s,
    border-color 0.15s;
  user-select: none;
  background-color: color-mix(in srgb, var(--calendar-accent) 11%, transparent);
}

.cal-cell.interactive {
  cursor: pointer;
}

.cal-cell.interactive:hover {
  background-color: var(--p-content-hover-background);
}

.cal-cell.other-month {
  opacity: 0.45;
}

.cal-cell.other-month:not(.interactive) {
  opacity: 0.25;
  pointer-events: none;
}

.cal-cell.is-today {
  background-color: var(--p-primary-color);
  color: var(--p-primary-contrast-color);
  font-weight: 700;
}

.cal-cell.is-today:hover {
  background-color: var(--p-primary-hover-color);
}

.cal-cell.has-reset {
  background-color: color-mix(in srgb, var(--red-500) 22%, transparent);
  border-color: color-mix(in srgb, var(--red-500) 65%, transparent);
}

.cal-cell.has-reset::after {
  content: '';
  position: absolute;
  top: 3px;
  right: 3px;
  width: 10px;
  height: 2px;
  border-radius: 999px;
  background-color: var(--red-500);
}

.cal-cell.is-today.has-reset {
  background-color: var(--red-500);
  color: #fff;
}

.cal-cell.disabled {
  opacity: 0.25;
  pointer-events: none;
}

.cal-day {
  line-height: 1;
}

.cal-reset-dot {
  position: absolute;
  bottom: 3px;
  width: 5px;
  height: 5px;
  border-radius: 50%;
  background-color: var(--red-500);
}

.cal-cell.is-today .cal-reset-dot {
  background-color: #fff;
}

.cal-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 10px 14px;
  margin-top: 10px;
  font-size: 0.75rem;
  color: var(--p-text-muted-color);
}

.cal-legend-item {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.cal-legend-swatch {
  width: 12px;
  height: 12px;
  border-radius: 4px;
  display: inline-block;
}

.cal-legend-swatch.clean {
  background-color: color-mix(in srgb, var(--calendar-accent) 11%, transparent);
}

.cal-legend-swatch.reset {
  background-color: color-mix(in srgb, var(--red-500) 22%, transparent);
  border: 1px solid color-mix(in srgb, var(--red-500) 65%, transparent);
}
</style>
