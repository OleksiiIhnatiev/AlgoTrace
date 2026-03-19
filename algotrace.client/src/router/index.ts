import { createRouter, createWebHistory } from 'vue-router';
import { authState, authService } from '../services/auth.service';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('../components/HomeView.vue')
    },
    {
      path: '/auth',
      name: 'auth',
      component: () => import('../components/AuthView.vue')
    },
    {
      path: '/analyzer',
      name: 'analyzer',
      component: () => import('../components/AnalyzerView.vue')
    },
    {
      path: '/export',
      name: 'export',
      component: () => import('../components/ReportExportView.vue')
    },
    {
      path: '/storage',
      name: 'storage',
      component: () => import('../components/StorageView.vue'),
      meta: { requiresAuth: true }
    }
  ]
});

router.beforeEach(async (to, from, next) => {
  if (authState.loading) {
    await authService.init();
  }

  if (to.meta.requiresAuth && !authState.isAuthenticated) {
    next('/auth');
  } else {
    next();
  }
});

export default router;
