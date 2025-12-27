import { createRouter, createWebHistory } from 'vue-router'
import MainLayout from '@/layouts/MainLayout.vue'

import TasksView from '@/views/TasksView.vue'
import HabbitsView from '@/views/HabbitsView.vue'
import GoalsView from '@/views/GoalsView.vue'
import JournalView from '@/views/JournalView.vue'
import ProfileView from '@/views/ProfileView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: MainLayout,
      children: [
        { path: '', redirect: '/tasks' },
        { path: 'tasks', component: TasksView },
        { path: 'habbits', component: HabbitsView },
        { path: 'goals', component: GoalsView },
        { path: 'journal', component: JournalView },
        { path: 'profile', component: ProfileView },
      ],
    },
  ],
})

export default router
