<script setup lang="ts">
import { onMounted, ref } from 'vue'
import GoalCard from '@/components/GoalCard.vue'
import { goalsApi } from '@/api/GoalsAPI'
import { habitsApi } from '@/api/HabitsAPI'
import { addictionsApi } from '@/api/AddictionsAPI'
import type { GoalItem } from '@/models/GoalItem'
import type { HabitWithHistoryDTO } from '@/api/HabitsAPI'
import type { AddictionWithResetsDTO } from '@/api/AddictionsAPI'
import { getTasks, type TaskDTO } from '@/api/TasksAPI'

const goals = ref<GoalItem[]>([])
const tasksMap = ref<Record<number, TaskDTO>>({})
const habitsMap = ref<Record<number, HabitWithHistoryDTO>>({})
const addictionsMap = ref<Record<number, AddictionWithResetsDTO>>({})

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
        addictions.map(addiction => [Number(addiction.addiction.id), addiction])
    )
})

</script>

<template>
    <GoalCard v-for="goal in goals" :key="goal.id" :goal="goal" :tasksMap="tasksMap" :habitsMap="habitsMap"
        :addictionsMap="addictionsMap" />
</template>
