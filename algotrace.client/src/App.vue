<script setup lang="ts">
import { onMounted } from 'vue';
import { RouterView } from 'vue-router';
import { authService } from './services/auth.service';

onMounted(() => {
  authService.init();
});
</script>

<template>
  <div class="main-wrapper">
    <RouterView v-slot="{ Component }">
      <transition name="fade" mode="out-in">
        <component :is="Component" />
      </transition>
    </RouterView>
  </div>
</template>

<style scoped>
.main-wrapper {
  overflow-x: hidden;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.4s ease, transform 0.4s ease;
}

.fade-enter-from {
  opacity: 0;
  transform: translateY(10px);
}

.fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
