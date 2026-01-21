import { useAuthStore } from '@/stores/auth';
import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/login",
      component: () => import("@/views/LoginView.vue")
    },
    {
      path: "/",
      component: () => import("@/layouts/MainLayout.vue"),
      meta: { requiresAuth: true },
      children: [
        { path: "", redirect: "/tasks" },
        { path: "tasks", component: () => import("@/views/TasksView.vue") },
        { path: "habits", component: () => import("@/views/HabitsView.vue") },
        { path: "addictions", component: () => import("@/views/AddiсtionsView.vue") },
        { path: "goals", component: () => import("@/views/GoalsView.vue") },
        { path: "journal", component: () => import("@/views/JournalView.vue") },
        { path: "profile", component: () => import("@/views/ProfileView.vue") },
      ]
    },
    {
      path: "/addictions/:id",
      name: "addiction-details",
      component: () => import("@/views/AddictionDetailsView.vue"),
      meta: { requiresAuth: true }
    }
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore();

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return "/login";
  }

  if (to.path === "/login" && auth.isAuthenticated) {
    return "/";
  }
});

export default router
