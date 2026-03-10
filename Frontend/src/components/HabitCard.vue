<script setup lang="ts">
import Card from 'primevue/card'
import HabitDayRow from './HabitDayRow.vue'
import type { HabitDTO, HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { computed } from 'vue'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'

const props = withDefaults(
  defineProps<{ habit: HabitWithHistoryDTO; noBorder?: boolean }>(),
  { noBorder: false }
)
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()
const areaColor = computed(() => lifeAreasStore.getAreaColorById(props.habit.habit.lifeAreaId))
const cardBorderStyle = computed(() => {
  if (props.noBorder) return { border: 'none', borderLeftWidth: 0 }
  return areaColor.value ? { borderLeftWidth: '4px', borderLeftStyle: 'solid', borderLeftColor: areaColor.value } : { borderLeftWidth: 0 }
})
const goalTitle = computed(() => goalsStore.getGoalById(props.habit.habit.goalId)?.title ?? null)

const emit = defineEmits<{
  (e: 'edit', habit: HabitDTO): void
}>()
</script>

<template>
    <Card class="habit-card" :class="{ 'no-border': noBorder }" :style="cardBorderStyle">
        <template #title>
            <div class="habit-title" @click="emit('edit', habit.habit)">
                {{ habit.habit.title }}
            </div>
        </template>

        <template #content>
            <div v-if="goalTitle" class="goal-text">
                {{ goalTitle }}
            </div>
            <HabitDayRow :habit="habit" />
        </template>
    </Card>
</template>

<style scoped>
.habit-card {
  border-radius: 16px;
  border-left-width: 4px;
  border-left-style: solid;
}

.habit-card.no-border {
  border: none;
  border-left-width: 0;
}

.habit-card.no-border :deep(.p-card) {
  border: none;
  box-shadow: none;
}

.habit-title {
    cursor: pointer;
    user-select: none;
}

.goal-text {
  color: var(--p-text-muted-color);
  font-size: 0.875rem;
  margin-bottom: 0.5rem;
}
</style>
