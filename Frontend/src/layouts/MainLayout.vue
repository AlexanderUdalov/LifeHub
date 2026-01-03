<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { computed } from 'vue'
import TabMenu from 'primevue/tabmenu'

const router = useRouter()
const route = useRoute()

const items = [
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
    items.findIndex(i => i.route === route.path)
)
</script>

<template>
    <div class="layout">
        <main class="content">
            <RouterView />
        </main>

        <TabMenu :model="items" :activeIndex="activeIndex" class="bottom-nav"
            @tab-change="e => items[e.index]?.route && router.push(items[e.index]!.route)" />
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
