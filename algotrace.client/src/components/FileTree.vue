<template>
  <aside class="sidebar-tree">
    <div class="tree-header">{{ title }}</div>
    <div class="tree-content">
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
.sidebar-tree { 
  width: 250px; 
  background: #ffffff;
  border-right: 1px solid #dee2e6; 
  border-left: 1px solid #dee2e6;
  display: flex; 
  flex-direction: column; 
}
.tree-header { 
  padding: 10px 15px; 
  font-size: 11px; 
  color: #6c757d; 
  background: #f8f9fa;
  text-transform: uppercase; 
  font-weight: bold; 
  border-bottom: 1px solid #dee2e6; 
}
.tree-content { 
  flex: 1; 
  overflow-y: auto; 
  padding: 10px 5px; 
}

.tree-content::-webkit-scrollbar { width: 8px; }
.tree-content::-webkit-scrollbar-thumb { background: #dee2e6; border-radius: 4px; }
.tree-content::-webkit-scrollbar-thumb:hover { background: #adb5bd; }
</style>