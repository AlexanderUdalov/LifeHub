<script setup lang="ts">
import { computed, ref } from 'vue'
import type { GoalItem } from '@/models/GoalItem'
import Card from 'primevue/card'
import ProgressBar from 'primevue/progressbar'
import Button from 'primevue/button'
import TaskCard from './TaskCard.vue'
import HabitCard from './HabitCard.vue'
import AddictionCard from './AddictionCard.vue'
import type { HabitWithHistoryDTO } from '@/api/HabitsAPI'
import type { AddictionItem } from '@/models/AddictionItem'
import type { TaskDTO } from '@/api/TasksAPI'

const props = defineProps<{
    goal: GoalItem
    tasksMap: Record<number, TaskDTO>
    habitsMap: Record<number, HabitWithHistoryDTO>
    addictionsMap: Record<number, AddictionItem>
}>()

function daysUntil(date: Date) {
    const now = new Date()
    const diff = date.getTime() - now.getTime()
    return Math.ceil(diff / (1000 * 60 * 60 * 24))
}

const dueText = computed(() => {
    const days = daysUntil(props.goal.dueDate)
    if (days < 0) return 'Overdue'
    if (days === 0) return 'Due today'
    return `Due in ${days} days`
})

const expanded = ref(false)

function toggle() {
    expanded.value = !expanded.value
}
</script>

<template>
    <Card class="goal-card">
        <template #content>
            <!-- Header -->
            <div class="goal-header" @click="toggle">
                <div class="area-icon" :style="{ background: goal.area.color }">
                    <i :class="goal.area.icon" />
                </div>

                <div class="goal-title">
                    <div class="title">{{ goal.title }}</div>
                    <div class="due">
                        {{ goal.progress }}% complete
                    </div>
                </div>

                <Button icon="pi pi-chevron-down" class="p-button-text p-button-rounded chevron"
                    :class="{ rotated: expanded }" />
            </div>
            <!-- Progress -->
            <ProgressBar :value="goal.progress" class="goal-progress">{{ dueText }}</ProgressBar>

            <!-- Stats -->
            <div class="goal-stats">
                <span>📝 Tasks({{ goal.tasks.length }})</span>
                <span>🔁 Habits({{ goal.habits.length }})</span>
                <span>⚠️ Addiction({{ goal.addictions.length }})</span>
            </div>

            <!-- todo: remove ! -->
            <!-- Collapsible content -->
            <div v-if="expanded" class="goal-expanded">
                <div v-if="goal.tasks.length">
                    <h4>Tasks</h4>
                    <TaskCard v-for="id in goal.tasks" :key="id" :task="tasksMap[id]!" />
                </div>

                <div v-if="goal.habits.length">
                    <h4>Habits</h4>
                    <HabitCard v-for="id in goal.habits" :key="id" :habit="habitsMap[id]!" />
                </div>

                <div v-if="goal.addictions.length">
                    <h4>Addictions</h4>
                    <AddictionCard v-for="id in goal.addictions" :key="id" :addiction="addictionsMap[id]!" />
                </div>
            </div>
        </template>
    </Card>
</template>

<style scoped>
.goal-card {
    margin: 12px;
}

.goal-header {
    display: flex;
    align-items: center;
    gap: 12px;
    cursor: pointer;
}

.chevron {
    margin-left: auto;
    transition: transform 0.2s ease;
}

.chevron.rotated {
    transform: rotate(180deg);
}

.goal-expanded {
    margin-top: 12px;
}

.goal-expanded h4 {
    margin: 12px 0 6px;
    font-size: 0.9rem;
    color: var(--text-color-secondary);
}
</style>
