<script setup lang="ts">
import { onMounted, ref } from 'vue'
import GoalCard from '@/components/GoalCard.vue'
import { goalsApi } from '@/api/GoalsAPI'
import { habitsApi } from '@/api/HabitsAPI'
import { addictionsApi } from '@/api/AddictionAPI'
import type { GoalItem } from '@/models/GoalItem'
import type { HabitWithHistory } from '@/models/HabitItem'
import type { AddictionItem } from '@/models/AddictionItem'
import { getTasks, type TaskDTO } from '@/api/TasksAPI'

const goals = ref<GoalItem[]>([])
const tasksMap = ref<Record<number, TaskDTO>>({})
const habitsMap = ref<Record<number, HabitWithHistory>>({})
const addictionsMap = ref<Record<number, AddictionItem>>({})

onMounted(async () => {
    goals.value = await goalsApi.getGoals()
    const tasks = await getTasks()

    tasksMap.value = Object.fromEntries(
        tasks.map(task => [task.id, task])
    )

    const habits = await habitsApi.getHabits()

    habitsMap.value = Object.fromEntries(
        habits.map(habit => [habit.habit.id, habit])
    )

    const addictions = await addictionsApi.getAddictions()

    addictionsMap.value = Object.fromEntries(
        addictions.map(addiction => [addiction.id, addiction])
    )
})

</script>

<template>
    <GoalCard v-for="goal in goals" :key="goal.id" :goal="goal" :tasksMap="tasksMap" :habitsMap="habitsMap"
        :addictionsMap="addictionsMap" />
</template>
