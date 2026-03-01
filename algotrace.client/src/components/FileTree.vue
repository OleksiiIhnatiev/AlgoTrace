<template>
  <aside class="d-flex flex-column bg-white border-end border-start" style="width: 260px; flex-shrink: 0;">
    <div class="px-3 py-2 text-uppercase fw-bold text-secondary border-bottom" style="font-size: 11px; background-color: #f8f9fa;">
      {{ title }}
    </div>
    <div class="flex-grow-1 overflow-auto p-2 custom-scrollbar">
      <FileTreeNode 
        v-for="rootNode in items" 
        :key="rootNode.name" 
        :node="rootNode"
        :selectedFile="selectedFile"
        :dynamicScores="dynamicScores"
        @select="$emit('select', $event)"
      />
    </div>
  </aside>
</template>

<script setup>
import FileTreeNode from './FileTreeNode.vue';

defineProps(['title', 'items', 'selectedFile', 'dynamicScores']);
defineEmits(['select']);
</script>

<style scoped>
.custom-scrollbar::-webkit-scrollbar { width: 6px; }
.custom-scrollbar::-webkit-scrollbar-thumb { background: #dee2e6; border-radius: 4px; }
.custom-scrollbar::-webkit-scrollbar-thumb:hover { background: #adb5bd; }
</style>