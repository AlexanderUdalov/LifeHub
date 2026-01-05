<script setup lang="ts">
import { habitsApi } from '@/api/HabitsAPI'
import HabitCard from '@/components/HabitCard.vue'
import type { HabitWithHistory } from '@/models/HabitItem'
import { onMounted, ref } from 'vue'

const habits = ref<HabitWithHistory[]>([])

onMounted(async () => {
  habits.value = await habitsApi.getHabits()
})
</script>

<template>
    <div class="habits-view">
        <HabitCard v-for="h in habits" :key="h.habit.id" :habit="h" />
    </div>
</template>

<style scoped>
.habits-view {
    display: flex;
    flex-direction: column;
    gap: 12px;
    padding: 12px;
}
</style>
