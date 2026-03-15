import { ref, watch } from 'vue';

const storedTheme = localStorage.getItem('theme');
const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;

export const isDarkMode = ref(storedTheme === 'dark' || (!storedTheme && prefersDark));

export const toggleTheme = () => {
  isDarkMode.value = !isDarkMode.value;
};

watch(isDarkMode, (dark) => {
  localStorage.setItem('theme', dark ? 'dark' : 'light');
  document.documentElement.setAttribute('data-bs-theme', dark ? 'dark' : 'light');
}, { immediate: true });
