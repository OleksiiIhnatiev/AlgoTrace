<script setup lang="ts">
import { authService } from '@/services/auth.service';
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { isDarkMode, toggleTheme } from '../composables/useTheme';

const router = useRouter();
const isLogin = ref(true);
const loading = ref(false);
const error = ref('');

const form = reactive({
  email: '',
  password: ''
});

const toggleMode = () => {
  isLogin.value = !isLogin.value;
  error.value = '';
  form.email = '';
  form.password = '';
};

const handleSubmit = async () => {
  loading.value = true;
  error.value = '';

  try {
    if (isLogin.value) {
      await authService.login({
        email: form.email,
        password: form.password
      });
      await authService.init();
      router.push('/');
    } else {
      await authService.register({
        email: form.email,
        password: form.password
      });
      await authService.login({
        email: form.email,
        password: form.password
      });
      await authService.init();
      router.push('/');
    }
  } catch (err: unknown) {
    console.error(err);
    error.value = isLogin.value
      ? 'Невірний логін або пароль.'
      : 'Помилка реєстрації. Можливо, такий користувач вже існує.';
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <div class="min-vh-100 d-flex align-items-center justify-content-center bg-light position-relative">
    <div class="position-absolute top-0 end-0 p-4 z-3">
       <button @click="toggleTheme" class="btn btn-light rounded-circle shadow-sm border hover-lift d-flex align-items-center justify-content-center" style="width: 44px; height: 44px;" title="Змінити тему">
         <i class="bi fs-5" :class="isDarkMode ? 'bi-sun-fill text-warning' : 'bi-moon-stars-fill text-secondary'"></i>
       </button>
    </div>
    <div class="card border-0 shadow-lg rounded-4 overflow-hidden w-100" style="max-width: 400px;">
      <div class="card-body p-5">
        <div class="text-center mb-4">
          <div class="bg-primary bg-gradient text-white rounded-3 p-2 d-inline-flex align-items-center justify-content-center shadow-sm mb-3" style="width: 50px; height: 50px;">
            <i class="bi bi-layers-half fs-3"></i>
          </div>
          <h4 class="fw-bold text-dark">{{ isLogin ? 'З поверненням!' : 'Створити акаунт' }}</h4>
          <p class="text-muted small">Введіть свої дані для продовження</p>
        </div>

        <form @submit.prevent="handleSubmit">
          <div class="mb-3">
            <label class="form-label small fw-bold text-secondary">Email</label>
            <input v-model="form.email" type="email" class="form-control rounded-3 py-2" placeholder="name@example.com" required />
          </div>

          <div class="mb-4">
            <label class="form-label small fw-bold text-secondary">Пароль</label>
            <input v-model="form.password" type="password" class="form-control rounded-3 py-2" placeholder="••••••••" required />
          </div>

          <div v-if="error" class="alert alert-danger py-2 small rounded-3 mb-3">
            <i class="bi bi-exclamation-circle-fill me-1"></i> {{ error }}
          </div>

          <button type="submit" class="btn btn-primary w-100 rounded-pill fw-bold py-2 mb-4 shadow-sm" :disabled="loading">
            <span v-if="loading" class="spinner-border spinner-border-sm me-2"></span>
            {{ isLogin ? 'Увійти' : 'Зареєструватися' }}
          </button>

          <div class="text-center small">
            <span class="text-muted">{{ isLogin ? 'Немає акаунту?' : 'Вже є акаунт?' }}</span>
            <a href="#" class="text-primary fw-bold text-decoration-none ms-1" @click.prevent="toggleMode">
              {{ isLogin ? 'Зареєструватися' : 'Увійти' }}
            </a>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
