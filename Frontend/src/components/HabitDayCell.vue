<script setup lang="ts">
import { computed, ref } from 'vue'
import type { HabitCompletion, HabitWithHistory } from '@/models/HabitItem'

const props = defineProps<{
    date: Date
    habit: HabitWithHistory
}>()

const emit = defineEmits<{
    (e: 'update', date: Date, value: HabitCompletion): void
}>()

// todo: remove getDate
const entry = computed(() => props.habit.history.find(h => h.date.getDate() === props.date.getDate()))
const completion = computed<HabitCompletion>(() => entry.value?.completion ?? 'none')
const dayNumber = computed(() => props.date.getDate())
const isToday = computed(() => props.date === new Date())

// todo: streak
// const streak = computed(() =>
//   calculateStreak(props.habit.history, props.date)
// )

// const intensity = computed(() =>
//   Math.min(streak.value / 7, 1)
// )

function nextState() {
    const order: HabitCompletion[] = ['none', 'skip', 'full']
    const index = order.indexOf(completion.value)
    // todo: completion
    // completion.value = order[(index + 1) % order.length]
    emit('update', props.date, completion.value)
}
</script>

<template>
    <div class="cell-wrapper">
        <div class="day-label">{{ dayNumber }}</div>
        <div class="cell" :class="[completion, { today: isToday }]" :style="{ '--habit-color': habit.habit.color }"
            @click="nextState" />
    </div>
</template>

<style scoped>
.cell-wrapper {
    display: flex;
    flex-direction: column;
    align-items: center;
    flex-shrink: 0;
}

.day-label {
    font-size: 0.65rem;
    margin-bottom: 2px;
}

.cell {
    width: 28px;
    height: 28px;
    margin: 2px;
    border-radius: 6px;
    border: 1px solid;
    border-color: aliceblue;
    flex-shrink: 0;
    cursor: pointer;
    transition: transform 0.1s ease, background-color 0.2s;
}

.cell:active {
    transform: scale(0.9);
}

.cell.today {
    outline: 2px solid var(--habit-color);
}

.cell.full {
    background-color: var(--habit-color);
}

.cell.partial {
    background: linear-gradient(135deg,
            var(--habit-color) 50%,
            transparent 50%);
}

/* background-color: color-mix(
  in srgb,
  var(--habit-color) calc(var(--intensity) * 100%),
  transparent
); */
</style>
