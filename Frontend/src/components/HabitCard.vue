<script setup lang="ts">
import Card from 'primevue/card'
import HabitDayRow from './HabitDayRow.vue'
import type { HabitDTO, HabitWithHistoryDTO } from '@/api/HabitsAPI'
import { computed } from 'vue'
import { useLifeAreasStore } from '@/stores/lifeAreas'

const props = defineProps<{ habit: HabitWithHistoryDTO }>()
const lifeAreasStore = useLifeAreasStore()
const areaColor = computed(() => lifeAreasStore.getAreaColorById(props.habit.habit.lifeAreaId))
const cardBorderStyle = computed(() =>
  areaColor.value ? { borderLeftWidth: '4px', borderLeftStyle: 'solid', borderLeftColor: areaColor.value } : { borderLeftWidth: 0 }
)

const emit = defineEmits<{
  (e: 'edit', habit: HabitDTO): void
}>()
</script>

<template>
    <Card class="habit-card" :style="cardBorderStyle">
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
  border-left-width: 4px;
  border-left-style: solid;
}

.habit-title {
    cursor: pointer;
    user-select: none;
}
</style>
