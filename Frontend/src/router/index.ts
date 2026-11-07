import { useAuthStore } from '@/stores/auth'
import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  scrollBehavior(to) {
    return to.hash ? { el: to.hash, top: 80, behavior: 'smooth' } : { top: 0 }
  },
  routes: [
    {
      path: '/login',
      component: () => import('@/views/LoginView.vue'),
    },
    {
      path: '/',
      component: () => import('@/layouts/MainLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: '', redirect: '/today' },
        {
          path: 'today',
          component: () => import('@/views/TodayView.vue'),
          meta: { titleKey: 'redesign.today' },
        },
        { path: 'more', component: () => import('@/views/MoreView.vue') },
        {
          path: 'appearance',
          component: () => import('@/views/AppearanceView.vue'),
          meta: { titleKey: 'redesign.appearance' },
        },
        {
          path: 'navigation',
          component: () => import('@/views/NavigationView.vue'),
          meta: { titleKey: 'redesign.navigation' },
        },
        {
          path: 'tasks',
          component: () => import('@/views/TasksView.vue'),
          meta: { titleKey: 'tasks.tasks' },
        },
        {
          path: 'habits',
          component: () => import('@/views/HabitsView.vue'),
          meta: { titleKey: 'habits.habits' },
        },
        {
          path: 'addictions',
          component: () => import('@/views/AddiсtionsView.vue'),
          meta: { titleKey: 'addictions.addictions' },
        },
        {
          path: 'lifeareas',
          component: () => import('@/views/LifeAreasView.vue'),
          meta: { titleKey: 'lifeareas.title' },
        },
        {
          path: 'goals',
          component: () => import('@/views/GoalsView.vue'),
          meta: { titleKey: 'goals.title' },
        },
        {
          path: 'journal',
          component: () => import('@/views/JournalView.vue'),
          meta: { titleKey: 'journal.title' },
        },
        { path: 'profile', component: () => import('@/views/ProfileView.vue') },
      ],
    },
    {
      path: '/addictions/:id',
      name: 'addiction-details',
      redirect: (to) => ({ path: '/addictions', hash: '#tracker-' + to.params.id }),
      meta: { requiresAuth: true },
    },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return '/login'
  }

  if (to.path === '/login' && auth.isAuthenticated) {
    return '/'
  }
})

export default router
