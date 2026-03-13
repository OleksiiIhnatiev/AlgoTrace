<script setup lang="ts">
import { ref } from 'vue';
import api from '@/services/api';

interface Node {
  id: number;
  name: string;
  type: 'folder' | 'file';
}

const props = defineProps<{
  node: Node;
  selectedIds: number[];
}>();

const emit = defineEmits(['toggle-selection']);

const isOpen = ref(false);
const children = ref<Node[]>([]);
const isLoading = ref(false);

const toggleFolder = async () => {
  if (props.node.type !== 'folder') return;
  isOpen.value = !isOpen.value;

  if (isOpen.value && children.value.length === 0) {
    isLoading.value = true;
    try {
      const res = await api.get(`/api/directory/folder/${props.node.id}`);
      const folders = res.data.folders.map((f: { folderId: number; name: string }) => ({ id: f.folderId, name: f.name, type: 'folder' }));
      const files = res.data.files.map((f: { fileId: number; name: string }) => ({ id: f.fileId, name: f.name, type: 'file' }));
      children.value = [...folders, ...files];
    } catch (err) {
      console.error("Error loading subfolder:", err);
    } finally {
      isLoading.value = false;
    }
  }
};
</script>

<template>
  <div class="ms-3">
    <div class="d-flex align-items-center py-1 hover-bg rounded px-2">
      <span @click="toggleFolder" class="me-1 cursor-pointer" style="width: 20px;">
        <i v-if="node.type === 'folder'" :class="isOpen ? 'bi bi-chevron-down' : 'bi bi-chevron-right'" class="small text-muted"></i>
      </span>
      <input type="checkbox" 
             :checked="selectedIds.includes(node.id)" 
             @change="emit('toggle-selection', node.id)"
             class="form-check-input me-2 mt-0">
      <div @click="toggleFolder" class="d-flex align-items-center cursor-pointer flex-grow-1">
        <i :class="node.type === 'folder' ? 'bi bi-folder-fill text-warning' : 'bi bi-file-earmark-code text-primary'" class="me-2"></i>
        <span class="small fw-medium">{{ node.name }}</span>
        <div v-if="isLoading" class="spinner-border spinner-border-sm text-secondary ms-2"></div>
      </div>
    </div>
    <div v-if="isOpen && children.length > 0">
      <FileTreeItem v-for="child in children" :key="child.id" :node="child" :selected-ids="selectedIds" @toggle-selection="(id) => emit('toggle-selection', id)" />
    </div>
  </div>
</template>

<style scoped>
.hover-bg:hover { background-color: rgba(0, 123, 255, 0.05); }
.cursor-pointer { cursor: pointer; }
</style>