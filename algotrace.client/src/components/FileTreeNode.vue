<template>
  <div class="tree-node">
    <div v-if="node.type === 'folder' || node.children" class="folder-group">
      <div class="folder-label" @click="isOpen = !isOpen">
        <span class="folder-icon">{{ isOpen ? '📂' : '📁' }}</span>
        {{ node.name }}
      </div>
      <div v-show="isOpen" class="folder-children">
        <FileTreeNode 
          v-for="child in node.children" 
          :key="child.name" 
          :node="child"
          :selectedFile="selectedFile"
          :dynamicScores="dynamicScores"
          @select="$emit('select', $event)"
        />
      </div>
    </div>

    <div v-else
         class="file-row"
         :class="{'active': selectedFile === node.name}"
         @click="$emit('select', node)">
      <span class="file-name">📄 {{ node.name }}</span>
      <span v-if="getScore(node)" class="file-score" :style="{ color: scoreColor(getScore(node)) }">
        {{ getScore(node) }}%
      </span>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';

const props = defineProps(['node', 'selectedFile', 'dynamicScores']);
defineEmits(['select']);

const isOpen = ref(true);

const getScore = (file) => {
  if (props.dynamicScores && props.dynamicScores[file.name] !== undefined) {
    return props.dynamicScores[file.name];
  }
  return file.score;
};

const scoreColor = (s) => s > 75 ? '#f44336' : s > 40 ? '#ff9800' : '#4caf50';
</script>

<style scoped>
.tree-node {
  font-size: 13px;
  color: #495057;
}

.folder-label {
  padding: 4px 6px;
  cursor: pointer;
  color: #6c757d;
  user-select: none;
  display: flex;
  align-items: center;
  gap: 6px;
}

.folder-label:hover {
  background: rgba(0, 0, 0, 0.04);
}

.folder-icon { font-size: 11px; }

.folder-children {
  padding-left: 8px;
  border-left: 1px solid #dee2e6;
  margin-left: 6px;
}

.file-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 4px 6px;
  margin: 1px 0;
  cursor: pointer;
  border-radius: 4px;
  transition: background 0.1s;
  min-width: 0;
}

.file-row:hover {
  background: rgba(0, 0, 0, 0.04);
}

.file-row.active {
  background: rgba(13, 110, 253, 0.1);
  color: #0d6efd;
  font-weight: 500;
}

.file-name {
  display: flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  min-width: 0;
  flex: 1;
}

.file-score {
  font-size: 11px;
  font-weight: bold;
  flex-shrink: 0;
  margin-left: 8px;
}
</style>