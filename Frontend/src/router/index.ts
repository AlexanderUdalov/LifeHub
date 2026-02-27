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
        { path: "tasks", component: () => import("@/views/TasksView.vue"), meta: { titleKey: "tasks.tasks" } },
        { path: "habits", component: () => import("@/views/HabitsView.vue"), meta: { titleKey: "habits.habits" } },
        { path: "addictions", component: () => import("@/views/AddiсtionsView.vue"), meta: { titleKey: "addictions.addictions" } },
        { path: "lifeareas", component: () => import("@/views/LifeAreasView.vue"), meta: { titleKey: "lifeareas.title" } },
        { path: "goals", component: () => import("@/views/GoalsView.vue"), meta: { titleKey: "goals.title" } },
        { path: "journal", component: () => import("@/views/JournalView.vue"), meta: { titleKey: "journal.title" } },
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
