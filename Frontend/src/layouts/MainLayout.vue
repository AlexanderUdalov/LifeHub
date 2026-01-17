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

type CreateType = 'task' | 'habit' | 'addiction' | 'goal' | null

const createType = ref<CreateType>(null)

const router = useRouter()
const route = useRoute()

const tabMenuItems = [
    {
        label: 'Tasks',
        icon: 'pi pi-check-square',
        route: '/tasks',
    },
    {
        label: 'Habbits',
        icon: 'pi pi-sync',
        route: '/habbits',
    },
    {
        label: 'Addiсtions',
        icon: 'pi pi-ban',
        route: '/addiсtions',
    },
    {
        label: 'Goals',
        icon: 'pi pi-flag',
        route: '/goals',
    },
    {
        label: 'Journal',
        icon: 'pi pi-book',
        route: '/journal',
    },
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
        command: () => (createType.value = 'task')
    },
    {
        label: 'Habit',
        icon: 'pi pi-calendar',
        command: () => (createType.value = 'habit')
    },
    {
        label: 'Addiction',
        icon: 'pi pi-ban',
        command: () => (createType.value = 'addiction')
    },
    {
        label: 'Goal',
        icon: 'pi pi-flag',
        command: () => (createType.value = 'goal')
    }
]

const goals = ref<GoalItem[]>([])
onMounted(async () => {
    goals.value = await goalsApi.getGoals()
})
</script>

<template>
    <div class="layout">
        <main class="content">
            <RouterView />
        </main>

        <SpeedDial :model="fabItems" direction="up" style="position: fixed; right: calc(50% - 190px); bottom: 110px"
            :buttonProps="{ rounded: true }" />

        <TaskEditDialog v-if="createType === 'task'" :task="null" :goals="goals" @close="createType = null" />
        <HabitEditDialog v-if="createType === 'habit'" :habit="null" :goals="goals" @close="createType = null" />
        <AddictionEditDialog v-if="createType === 'addiction'" :addiction="null" :goals="goals"
            @close="createType = null" />
        <GoalEditDialog v-if="createType === 'goal'" :goal="null" :goals="goals" @close="createType = null" />

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
</style>
