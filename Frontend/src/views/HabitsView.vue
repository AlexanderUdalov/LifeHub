<script setup lang="ts">
defineOptions({ name: 'HabitsView' })
import EmptyState from '@/components/EmptyState.vue'
import HabitCard from '@/components/HabitCard.vue'
import Skeleton from 'primevue/skeleton'
import { onMounted } from 'vue'
import { useHabitsStore } from '@/stores/habits'
import type { HabitDTO } from '@/api/HabitsAPI'

const habitsStore = useHabitsStore()

const emit = defineEmits<{
    (e: 'edit-habit', habit: HabitDTO): void
}>()

onMounted(async () => {
    await habitsStore.fetchHabits(56)
})
</script>

<template>
    <div class="habits-view">
        <h1 class="ds-page-header">{{ $t('habits.habits') }}</h1>

        <div v-if="habitsStore.isLoading && habitsStore.habits.length === 0" class="habits-skeleton">
            <div v-for="i in 4" :key="i" class="skeleton-card">
                <Skeleton shape="circle" size="2.5rem" class="skeleton-avatar" />
                <div class="skeleton-lines">
                    <Skeleton width="70%" height="1.25rem" />
                    <Skeleton width="50%" height="1rem" />
                </div>
            </div>
        </div>

        <EmptyState v-else-if="habitsStore.habits.length === 0" icon="pi pi-sync"
            :title="$t('habits.empty')" :subtitle="$t('habits.emptySubtitle')" />

        <HabitCard v-else v-for="h in habitsStore.habitsSorted" :key="h.habit.id" :habit="h"
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

.habits-skeleton {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.skeleton-card {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 1rem;
    border-radius: var(--p-border-radius);
    background: var(--p-card-background);
    border: 1px solid var(--p-card-border-color);
}

.skeleton-avatar {
    flex-shrink: 0;
}

.skeleton-lines {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    flex: 1;
}

</style>
