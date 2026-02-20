<script setup lang="ts">
import { computed } from 'vue'
import type { HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { toDateOnlyString } from '@/utils/dateOnly'

type HabitCompletion = 'none' | 'skip' | 'full'

const props = defineProps<{
  date: Date
  habit: HabitWithHistoryDTO
  disabled: boolean
  strength: string
}>()

const emit = defineEmits<{
  (e: 'update', date: Date, value: HabitCompletion): void
}>()

const entry = computed(() => {
  const key = toDateOnlyString(props.date)
  return props.habit.history.find(h => h.date === key)
})
const completion = computed<HabitCompletion>(() => {
  const raw = (entry.value?.status ?? 'none').toLowerCase()
  return raw === 'full' || raw === 'skip' || raw === 'none' ? raw : 'none'
})
function nextState() {
  if (props.disabled) return

  // UX: first click marks as done, then skip, then none.
  const order: HabitCompletion[] = ['none', 'full', 'skip']
  const index = order.indexOf(completion.value)
  const next = order[(index + 1) % order.length] ?? 'none'
  emit('update', props.date, next)
}
</script>

<template>
  <div class="cell-wrapper">
    <div class="cell" :class="[completion, { disabled: disabled }]"
      :style="{ '--habit-color': habit.habit.color, '--habit-strength': strength }" @click="nextState" />
  </div>
</template>

<style scoped>
.cell-wrapper {
  display: flex;
  flex-shrink: 0;
}

.cell {
  width: 28px;
  height: 28px;
  margin: 2px;
  border-radius: 6px;
  border: 1px solid var(--p-content-border-color);
  flex-shrink: 0;
  cursor: pointer;
  transition: transform 0.1s ease, background-color 0.2s;
  background-color: transparent;
}

.cell:active {
  transform: scale(0.9);
}

.cell.none {
  background-color: transparent;
  border-color: var(--p-content-border-color);
}

.cell.full {
  background-color: color-mix(in srgb, var(--habit-color) var(--habit-strength), transparent);
  border-color: color-mix(in srgb, var(--habit-color) var(--habit-strength), transparent);
}

.cell.skip {
  border-color: var(--habit-color);
}

.cell.disabled {
  cursor: default;
  pointer-events: none;
  opacity: 0.25;
  background-color: transparent;
  border-color: var(--p-content-border-color);
  outline: none;
}
</style>
