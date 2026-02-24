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
        <h1 class="view-page-header">{{ $t('habits.habits') }}</h1>
        <HabitCard v-for="h in habitsStore.habitsSorted" :key="h.habit.id" :habit="h"
            @edit="(habit) => emit('edit-habit', habit)" />
    </div>
</template>

<style scoped>
.habits-view {
    display: flex;
    flex-direction: column;
    gap: 12px;
    padding: 0 12px 12px;
}

.view-page-header {
  font-size: var(--p-card-title-font-size);
  font-weight: 600;
  text-align: center;
}
</style>
