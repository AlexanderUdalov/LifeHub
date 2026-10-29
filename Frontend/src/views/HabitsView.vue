<script setup lang="ts">
defineOptions({ name: 'HabitsView' })
import EmptyState from '@/components/EmptyState.vue'
import HabitCard from '@/components/HabitCard.vue'
import Button from 'primevue/button'
import Skeleton from 'primevue/skeleton'
import { onMounted } from 'vue'
import { useHabitsStore } from '@/stores/habits'
import type { HabitDTO } from '@/api/HabitsAPI'

const habitsStore = useHabitsStore()

const emit = defineEmits<{
    (e: 'edit-habit', habit: HabitDTO | null): void
}>()

onMounted(async () => {
    await habitsStore.fetchHabits(56)
})
</script>

<template>
    <div class="habits-view">
        <header class="habits-view__header">
            <h1 class="ds-page-header">{{ $t('habits.habits') }}</h1>
            <Button :label="$t('habits.editdialog.newHabit')" icon="pi pi-plus" class="desktop-create-btn"
                @click="emit('edit-habit', null)" />
        </header>

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

.habits-view__header {
    display: contents;
}

.desktop-create-btn {
    display: none;
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

@media (min-width: 900px) {
    .habits-view {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(22rem, 1fr));
        align-items: start;
        gap: 1rem;
        max-width: 72rem;
        margin: 0 auto;
        padding: 0;
    }

    .habits-view__header,
    .habits-skeleton,
    .habits-view :deep(.empty-state) {
        grid-column: 1 / -1;
    }

    .habits-view__header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
    }

    .desktop-create-btn {
        display: inline-flex;
    }

    .habits-view :deep(.empty-state) {
        justify-self: center;
    }

    .habits-skeleton {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(22rem, 1fr));
        gap: 1rem;
    }
}
</style>
