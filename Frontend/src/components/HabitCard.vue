<script setup lang="ts">
import Card from 'primevue/card'
import HabitDayRow from './HabitDayRow.vue'
import type { HabitWithHistory } from '@/models/HabitItem'

const props = defineProps<{ habit: HabitWithHistory }>()
</script>

<template>
    <Card class="habit-card">
        <template #title>
            {{ habit.habit.title }}
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
</style>
