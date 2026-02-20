<script setup lang="ts">
import Card from 'primevue/card'
import HabitDayRow from './HabitDayRow.vue'
import type { HabitDTO, HabitWithHistoryDTO } from '@/api/HabitsAPI'

const props = defineProps<{ habit: HabitWithHistoryDTO }>()

const emit = defineEmits<{
  (e: 'edit', habit: HabitDTO): void
}>()
</script>

<template>
    <Card class="habit-card">
        <template #title>
            <div class="habit-title" @click="emit('edit', habit.habit)">
                {{ habit.habit.title }}
            </div>
        </template>

        <template #content>
            <HabitDayRow :habit="habit" />
        </template>
    </Card>
</template>

<style scoped>
.habit-card {
    border-radius: 16px;
}

.habit-title {
    cursor: pointer;
    user-select: none;
}
</style>
