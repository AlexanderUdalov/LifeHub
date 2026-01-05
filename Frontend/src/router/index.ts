import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: () => import('@/layouts/MainLayout.vue'),
      children: [
        { path: '', redirect: '/tasks' },
        { path: 'tasks', component: () => import('@/views/TasksView.vue') },
        { path: 'habbits', component: () => import('@/views/HabitsView.vue') },
        { path: 'addiсtions', component: () => import('@/views/AddiсtionsView.vue') },
        { path: 'goals', component: () => import('@/views/GoalsView.vue') },
        { path: 'journal', component: () => import('@/views/JournalView.vue') },
        { path: 'profile', component: () => import('@/views/ProfileView.vue') },
        {
          path: '/addictions/:id',
          name: 'addiction-details',
          component: () => import('@/views/AddictionDetailsView.vue')
        }
      ],
    },
  ],
})

export default router
