<script setup lang="ts">
import HabitDayCell from './HabitDayCell.vue'
import type { HabitCompletion, HabitWithHistory } from '@/models/HabitItem'
import { computed, onMounted, ref } from 'vue'

const props = defineProps<{
    habit: HabitWithHistory
}>()

const days = computed(() => {
    const result = []
    for (let i = 0; i < 30; i++) {
        const d = new Date()
        d.setDate(d.getDate() - i)
        result.push(d)
    }
    return result.reverse()
})

const rowRef = ref<HTMLDivElement | null>(null)
onMounted(() => {
    if (rowRef.value) {
        rowRef.value.scrollLeft = rowRef.value.scrollWidth
    }
})

function onUpdate(date: Date, value: HabitCompletion) {
    
}
</script>

<template>
    <div ref="rowRef" class="day-row">
        <HabitDayCell v-for="day in days" :key="day.getDate()" :date="day" :habit="habit" @update="onUpdate" />
    </div>
</template>

<style scoped>
.day-row {
    display: flex;
    overflow-x: auto;
    padding-bottom: 4px;
    scrollbar-width: none;
}

.day-row::-webkit-scrollbar {
    display: none;
}
</style>
