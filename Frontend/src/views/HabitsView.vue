<script setup lang="ts">
import HabitCard from '@/components/HabitCard.vue'
import { onMounted } from 'vue'
import { useHabitsStore } from '@/stores/habits'
import type { HabitDTO } from '@/api/HabitsAPI'

const habitsStore = useHabitsStore()

const emit = defineEmits<{
    (e: 'edit-habit', habit: HabitDTO): void
}>()

onMounted(async () => {
    await habitsStore.fetchHabits(14)
})
</script>

<template>
    <div class="habits-view">
        <HabitCard v-for="h in habitsStore.habitsSorted" :key="h.habit.id" :habit="h"
            @edit="(habit) => emit('edit-habit', habit)" />
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
