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

<style>
/* Global Dark Mode Overrides */
[data-bs-theme="dark"] body {
  background-color: #121212 !important;
  color: #e0e0e0 !important;
}
[data-bs-theme="dark"] .bg-white {
  background-color: rgba(30, 30, 30, var(--bs-bg-opacity, 1)) !important;
}
[data-bs-theme="dark"] .bg-light {
  background-color: rgba(42, 42, 42, var(--bs-bg-opacity, 1)) !important;
}
[data-bs-theme="dark"] .text-dark {
  color: #f8f9fa !important;
}
[data-bs-theme="dark"] .text-secondary {
  color: #adb5bd !important;
}
[data-bs-theme="dark"] .text-muted {
  color: #868e96 !important;
}
[data-bs-theme="dark"] .border,
[data-bs-theme="dark"] .border-bottom,
[data-bs-theme="dark"] .border-top,
[data-bs-theme="dark"] .border-start,
[data-bs-theme="dark"] .border-end {
  border-color: rgba(255, 255, 255, 0.1) !important;
}
[data-bs-theme="dark"] .glass-card {
  background: rgba(30, 30, 30, 0.75) !important;
  border: 1px solid rgba(255, 255, 255, 0.08) !important;
  box-shadow: 0 10px 40px -10px rgba(0, 0, 0, 0.5) !important;
}
[data-bs-theme="dark"] .glass-panel {
  background: rgba(40, 40, 40, 0.5) !important;
  border: 1px solid rgba(255, 255, 255, 0.05) !important;
}
[data-bs-theme="dark"] .dashboard-bg {
  background-color: #121212 !important;
  background-image:
    radial-gradient(at 0% 0%, rgba(13, 110, 253, 0.15) 0px, transparent 50%),
    radial-gradient(at 100% 0%, rgba(220, 53, 69, 0.1) 0px, transparent 50%),
    radial-gradient(at 50% 100%, rgba(16, 185, 129, 0.1) 0px, transparent 50%) !important;
}
[data-bs-theme="dark"] .form-control,
[data-bs-theme="dark"] .form-select,
[data-bs-theme="dark"] textarea {
  background-color: #2a2a2a !important;
  border-color: rgba(255, 255, 255, 0.1) !important;
  color: #e0e0e0 !important;
}
[data-bs-theme="dark"] .btn-light,
[data-bs-theme="dark"] .btn-white {
  background-color: #2a2a2a !important;
  border-color: rgba(255, 255, 255, 0.1) !important;
  color: #e0e0e0 !important;
}
[data-bs-theme="dark"] .btn-light:hover,
[data-bs-theme="dark"] .btn-white:hover {
  background-color: #3a3a3a !important;
  color: #fff !important;
}
[data-bs-theme="dark"] .navbar {
  background-color: rgba(30, 30, 30, 0.8) !important;
  border-bottom-color: rgba(255, 255, 255, 0.05) !important;
}
[data-bs-theme="dark"] .card {
  background-color: #1e1e1e !important;
  border-color: rgba(255, 255, 255, 0.08) !important;
}
[data-bs-theme="dark"] .dropdown-menu {
  background-color: #2a2a2a;
  border-color: rgba(255, 255, 255, 0.1);
}
[data-bs-theme="dark"] .dropdown-item {
  color: #e0e0e0;
}
[data-bs-theme="dark"] .dropdown-item:hover {
  background-color: rgba(255, 255, 255, 0.1);
}
[data-bs-theme="dark"] .list-group-item {
  background-color: transparent !important;
  color: #e0e0e0 !important;
  border-color: rgba(255, 255, 255, 0.05) !important;
}
[data-bs-theme="dark"] pre,
[data-bs-theme="dark"] code {
  color: #e0e0e0 !important;
  background-color: transparent !important;
}
[data-bs-theme="dark"] .accordion-button {
  background-color: rgba(255, 255, 255, 0.05) !important;
  color: #e0e0e0 !important;
}
[data-bs-theme="dark"] .accordion-body {
  background-color: rgba(30, 30, 30, 0.7) !important;
  color: #e0e0e0 !important;
}
</style>
