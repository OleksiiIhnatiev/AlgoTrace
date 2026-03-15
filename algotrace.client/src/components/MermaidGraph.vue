<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue';
import mermaid from 'mermaid';
import { isDarkMode } from '../composables/useTheme';

const props = defineProps<{
  definition: string;
}>();

const svgContent = ref('');
const hasError = ref(false);
const graphId = 'mermaid_' + Math.random().toString(36).substr(2, 9);

const render = async () => {
  if (!props.definition || props.definition.trim() === 'graph TD') {
    svgContent.value = '<div class="text-muted small py-3">Немає даних для побудови графа</div>';
    return;
  }
  hasError.value = false;
  try {
    mermaid.initialize({ startOnLoad: false, theme: isDarkMode.value ? 'dark' : 'default' });
    const { svg } = await mermaid.render(graphId, props.definition);
    svgContent.value = svg;
  } catch (e) {
    console.error('Mermaid render error:', e);
    hasError.value = true;
  }
};

onMounted(() => nextTick(render));
watch(() => props.definition, () => nextTick(render));
watch(isDarkMode, () => nextTick(render));
</script>

<template>
  <div class="mermaid-container w-100 d-flex justify-content-center overflow-auto custom-scrollbar">
    <div v-if="hasError" class="text-danger small py-3 fw-bold">
      <i class="bi bi-exclamation-triangle me-1"></i> Не вдалося відмалювати граф (занадто складна структура)
    </div>
    <div v-else v-html="svgContent" class="w-100 d-flex justify-content-center p-2"></div>
  </div>
</template>
