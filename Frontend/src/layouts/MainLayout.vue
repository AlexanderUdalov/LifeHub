<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { computed, ref } from 'vue'
import SpeedDial from 'primevue/speeddial'
import TabMenu from 'primevue/tabmenu'

import TaskEditDialog from '@/components/TaskEditDialog.vue'
import HabitEditDialog from '@/components/HabitEditDialog.vue'
import AddictionEditDialog from '@/components/AddictionEditDialog.vue'
import GoalEditDialog from '@/components/GoalEditDialog.vue'

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

const activeIndex = computed(() =>
    tabMenuItems.findIndex(i => i.route === route.path)
)

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
</script>

<template>
    <div class="layout">
        <main class="content">
            <RouterView />
        </main>

        <SpeedDial :model="fabItems" direction="up" style="position: absolute; right: calc(50% - 200px); bottom: 60px"
            :buttonProps="{ rounded: true }" />

        <TaskEditDialog v-if="createType === 'task'" :task="null" @close="createType = null" />
        <HabitEditDialog v-if="createType === 'habit'" :habit="null" @close="createType = null" />
        <AddictionEditDialog v-if="createType === 'addiction'" :addiction="null" @close="createType = null" />
        <GoalEditDialog v-if="createType === 'goal'" :goal="null" @close="createType = null" />

        <TabMenu :model="tabMenuItems" :activeIndex="activeIndex" class="bottom-nav"
            @tab-change="e => tabMenuItems[e.index]?.route && router.push(tabMenuItems[e.index]!.route)" />
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

.bottom-nav {
    position: sticky;
    bottom: 0;
    width: auto;
    background-color: blue;
}
</style>
