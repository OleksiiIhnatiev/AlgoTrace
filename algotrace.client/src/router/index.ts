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
    }
  ]
});

// Глобальный Guard для защиты роутов
router.beforeEach(async (to, from, next) => {
  // Если данные еще грузятся (например, при F5), ждем инициализации
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
