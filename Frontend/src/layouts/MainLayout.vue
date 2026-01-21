<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { onMounted, ref } from 'vue'
import SpeedDial from 'primevue/speeddial'
import Tabs from 'primevue/tabs'
import Tab from 'primevue/tab'
import TabList from 'primevue/tablist'

import TaskEditDialog from '@/components/TaskEditDialog.vue'
import HabitEditDialog from '@/components/HabitEditDialog.vue'
import AddictionEditDialog from '@/components/AddictionEditDialog.vue'
import GoalEditDialog from '@/components/GoalEditDialog.vue'
import { goalsApi } from '@/api/GoalsAPI'
import type { GoalItem } from '@/models/GoalItem'
import type { HabitItem } from '@/models/HabitItem'
import type { AddictionItem } from '@/models/AddictionItem'
import { createTask, updateTask, type CreateTaskRequest, type TaskDTO, type UpdateTaskRequest } from '@/api/TasksAPI'

type ItemType = 'task' | 'habit' | 'addiction' | 'goal'
type EditContext =
    | { type: ItemType; item: null }
    | { type: ItemType; item: any }
    | null

const editContext = ref<EditContext>(null)

const router = useRouter()
const route = useRoute()

const tabMenuItems = [
    {
        label: 'Tasks',
        icon: 'pi pi-check-square',
        route: '/tasks',
    },
    // {
    //     label: 'Habbits',
    //     icon: 'pi pi-sync',
    //     route: '/habbits',
    // },
    // {
    //     label: 'Addiсtions',
    //     icon: 'pi pi-ban',
    //     route: '/addiсtions',
    // },
    // {
    //     label: 'Goals',
    //     icon: 'pi pi-flag',
    //     route: '/goals',
    // },
    // {
    //     label: 'Journal',
    //     icon: 'pi pi-book',
    //     route: '/journal',
    // },
    {
        label: 'Profile',
        icon: 'pi pi-user',
        route: '/profile',
    },
]

const fabItems = [
    {
        label: 'Task',
        icon: 'pi pi-check-square',
        command: () => (editContext.value = { type: 'task', item: null })
    },
    // {
    //     label: 'Habit',
    //     icon: 'pi pi-calendar',
    //     command: () => (editContext.value = { type: 'habit', item: null })
    // },
    // {
    //     label: 'Addiction',
    //     icon: 'pi pi-ban',
    //     command: () => (editContext.value = { type: 'addiction', item: null })
    // },
    // {
    //     label: 'Goal',
    //     icon: 'pi pi-flag',
    //     command: () => (editContext.value = { type: 'goal', item: null })
    // }
]

const goals = ref<GoalItem[]>([])
onMounted(async () => {
    goals.value = await goalsApi.getGoals()
})

</script>

<template>
    <div class="layout">
        <main class="content">
            <RouterView v-slot="{ Component }">
                <component :is="Component" @edit-task="(task: TaskDTO) => editContext = { type: 'task', item: task }"
                    @edit-habit="(habit: HabitItem) => editContext = { type: 'habit', item: habit }"
                    @edit-addiction="(addiction: AddictionItem) => editContext = { type: 'addiction', item: addiction }"
                    @edit-goal="(goal: GoalItem) => editContext = { type: 'goal', item: goal }" />
            </RouterView>
        </main>

        <SpeedDial :model="fabItems" direction="up" style="position: fixed; right: calc(50% - 190px); bottom: 110px"
            :buttonProps="{ rounded: true }" />

        <TaskEditDialog v-if="editContext && editContext.type === 'task'" :task="editContext.item" :goals="goals"
            @close="editContext = null" @update="(request: UpdateTaskRequest, id: string) => updateTask(id, request)"
            @create="(request: CreateTaskRequest) => createTask(request)" />
        <HabitEditDialog v-if="editContext && editContext.type === 'habit'" :habit="editContext.item" :goals="goals"
            @close="editContext = null" />
        <AddictionEditDialog v-if="editContext && editContext.type === 'addiction'" :addiction="editContext.item"
            :goals="goals" @close="editContext = null" />
        <GoalEditDialog v-if="editContext && editContext.type === 'goal'" :goal="editContext.item" :goals="goals"
            @close="editContext = null" />

        <Tabs :value="route.path" @update:value="(v) => router.push(v as string)">
            <TabList>
                <Tab v-for="tab in tabMenuItems" :key="tab.route" :value="tab.route" class="tab">
                    <i :class="tab.icon" />
                    <span>{{ tab.label }}</span>
                </Tab>
            </TabList>
        </Tabs>
    </div>
</template>

<style scoped>
.layout {
    display: flex;
    flex-direction: column;
    height: 100vh;
}

.content {
    flex: 1;
    overflow-y: auto;
}

.tab {
    flex: 1;
    min-height: 4px;

    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;

    gap: 4px;

    font-size: 0.6rem;
    color: var(--text-color-secondary);

    border-radius: 12px;
}

.tab:hover {
    background: rgba(0, 0, 0, 0.06);
    transition: background 0.2s ease;
}

:deep(.p-tabs) {
    position: fixed;
    bottom: 12px;
    left: 50%;
    transform: translateX(-50%);

    width: calc(100% - 24px);
    max-width: 420px;

    background: var(--surface-card);
    border-radius: 16px;

    box-shadow:
        0 8px 24px rgba(0, 0, 0, 0.12);

    padding: 6px 0;
}

:deep(.p-tablist) {
    border-radius: 10px;
    display: flex;
    justify-content: space-around;
}

:deep(.p-tab) {
    padding: 16px;
}

:deep(.p-tab.p-tab-active) {
    color: var(--p-primary-color);
}
</style>
