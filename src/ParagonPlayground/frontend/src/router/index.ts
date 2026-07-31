import { createRouter, createWebHistory } from 'vue-router';
import LoginPage from '../views/LoginPage.vue';
import DashboardPage from '../views/DashboardPage.vue';
import IntegrationsPage from '../views/IntegrationsPage.vue';
import FileExplorerPage from '../views/FileExplorerPage.vue';
import SettingsPage from '../views/SettingsPage.vue';
import ErrorPage from '../views/ErrorPage.vue';
import { useCurrentUser } from '../composables/useCurrentUser';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: LoginPage,
      meta: { public: true },
    },
    {
      path: '/',
      name: 'dashboard',
      component: DashboardPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/integrations',
      name: 'integrations',
      component: IntegrationsPage,
      meta: { requiresAuth: true, requiresAdmin: true },
    },
    {
      path: '/settings',
      name: 'settings',
      component: SettingsPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/files/:pathMatch(.*)*',
      name: 'files',
      component: FileExplorerPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/forbidden',
      name: 'forbidden',
      component: ErrorPage,
      props: {
        status: '403',
        heading: 'Forbidden',
        title: "You don't have access to this area",
        message:
          "Your account doesn't have permission to view this page. Contact your admin if you believe this is a mistake.",
      },
      meta: { requiresAuth: true },
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: ErrorPage,
      props: {
        status: '404',
        heading: 'Page Not Found',
        title: "We couldn't find that page",
        message: 'The URL may be incorrect, or the page may have been moved or removed.',
      },
      meta: { requiresAuth: true },
    },
  ],
});

router.beforeEach(async (to) => {
  const { currentUser, fetchCurrentUser } = useCurrentUser();

  if (currentUser.value === null) {
    await fetchCurrentUser();
  }

  if (to.meta.public) {
    return currentUser.value ? { name: 'dashboard' } : true;
  }

  if (currentUser.value === null) {
    return { name: 'login' };
  }

  if (to.meta.requiresAdmin && currentUser.value.role !== 'admin') {
    return { name: 'forbidden' };
  }
});

export default router;
