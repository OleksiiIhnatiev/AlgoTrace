import { createRouter, createWebHistory } from 'vue-router';
import HomeView from '../components/HomeView.vue';
import AuthView from '../components/AuthView.vue';
import AnalyzerPage from '../components/AnalyzerPage.vue';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView
    },
    {
      path: '/auth',
      name: 'auth',
      component: AuthView
    },
    {
      path: '/analyzer',
      name: 'analyzer',
      component: AnalyzerPage 
    }
  ]
});

export default router;
    }
  ]
});

export default router;
