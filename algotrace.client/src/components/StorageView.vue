<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { authState, authService } from '@/services/auth.service';
import api from '@/services/api';
import { isDarkMode, toggleTheme } from '../composables/useTheme';

interface FileEntry {
  fileId: string;
  name: string;
}

interface FolderEntry {
  folderId: string;
  name: string;
}

interface FolderContent {
  folderId: string | null;
  name: string;
  parentId: string | null;
  folders: FolderEntry[];
  files: FileEntry[];
}

const router = useRouter();

const currentFolderContent = ref<FolderContent | null>(null);
const selectedFile = ref<FileEntry | null>(null);
const fileContent = ref<string>('');
const isLoading = ref(false);
const isRoot = ref(true);

const newFolderName = ref('');
const fileInput = ref<HTMLInputElement | null>(null);
const editingId = ref<string | null>(null);
const renameValue = ref('');

const openFolder = async (folderId: string | null = null) => {
  isLoading.value = true;
  isRoot.value = folderId === null;
  try {
    const url = folderId ? `/api/directory/folder/${folderId}` : '/api/directory/folder';
    const res = await api.get<FolderContent>(url);
    currentFolderContent.value = res.data;
    selectedFile.value = null;
  } catch (err) {
    console.error("Access error:", err);
    if (!isRoot.value) alert("Помилка доступу до папки");
  } finally {
    isLoading.value = false;
  }
};

const triggerUpload = () => fileInput.value?.click();

const handleFileUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (!target.files || target.files.length === 0) return;

  const file: File | undefined = target.files[0];
  if (!file) return;

  const formData = new FormData();
  formData.append('file', file);

  const currentId = currentFolderContent.value?.folderId || '';
  const query = currentId ? `?folderId=${currentId}` : '';

  try {
    isLoading.value = true;
    await api.post(`/api/directory/file/upload${query}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    await openFolder(currentFolderContent.value?.folderId || null);
  } catch (err) {
    console.error("Upload error:", err);
    alert("Помилка завантаження файлу");
  } finally {
    isLoading.value = false;
    target.value = '';
  }
};

const viewFile = async (file: FileEntry) => {
  selectedFile.value = file;
  fileContent.value = 'Завантаження...';
  try {
    const res = await api.get<string>(`/api/directory/file/download/${file.fileId}`, {
      responseType: 'text'
    });
    fileContent.value = res.data;
  } catch (err) {
    console.error("View file error:", err);
    fileContent.value = 'Помилка завантаження вмісту.';
  }
};

const downloadFile = (fileId: string) => {
  window.open(`${api.defaults.baseURL}/api/directory/file/download/${fileId}`, '_blank');
};

const deleteFile = async (id: string) => {
  if (!confirm('Видалити цей файл?')) return;
  try {
    await api.delete(`/api/directory/file/${id}`);
    if (selectedFile.value?.fileId === id) selectedFile.value = null;
    await openFolder(currentFolderContent.value?.folderId || null);
  } catch (err) {
    console.error("Delete file error:", err);
    alert("Не вдалося видалити файл");
  }
};

const createNewFolder = async () => {
  if (!newFolderName.value.trim()) return;
  try {
    const parentId = currentFolderContent.value?.folderId || null;
    await api.post('/api/directory/folder', {
      name: newFolderName.value,
      parentId: parentId
    });
    newFolderName.value = '';
    await openFolder(parentId);
  } catch (err) {
    console.error("Create folder error:", err);
    alert("Не вдалося створити папку");
  }
};

const startRename = (id: string, currentName: string) => {
  editingId.value = id;
  renameValue.value = currentName;
};

const saveRename = async (id: string) => {
  if (!renameValue.value.trim()) return;
  try {
    await api.put(`/api/directory/folder/${id}/rename`, JSON.stringify(renameValue.value), {
        headers: { 'Content-Type': 'application/json' }
    });
    editingId.value = null;
    await openFolder(currentFolderContent.value?.folderId || null);
  } catch (err) {
    console.error("Rename error:", err);
    alert("Помилка перейменування");
  }
};

const deleteFolder = async (id: string) => {
  if (!confirm('Видалити папку та все вкладене?')) return;
  try {
    await api.delete(`/api/directory/folder/${id}`);
    await openFolder(currentFolderContent.value?.parentId || null);
  } catch (err) {
    console.error("Delete folder error:", err);
    alert("Не вдалося видалити папку");
  }
};

onMounted(() => {
  if (!authState.isAuthenticated) {
    router.push('/auth');
    return;
  }
  openFolder(null);
});
</script>

<template>
  <div class="min-vh-100 dashboard-bg pb-5 position-relative d-flex flex-column font-sans">
    <nav class="navbar navbar-expand-lg navbar-light bg-white bg-opacity-75 border-bottom py-3 backdrop-blur sticky-top z-3 shadow-sm">
      <div class="container-fluid px-4">
        <a class="navbar-brand fw-black d-flex align-items-center text-dark hover-lift" href="#" @click.prevent="router.push('/')" style="letter-spacing: -0.5px; transition: transform 0.2s;">
          <i class="bi bi-layers-half text-primary fs-3 me-2 glow-text-primary"></i>
          AlgoTrace <span class="text-primary ms-1">Nexus</span>
        </a>

        <div class="d-flex align-items-center">
          <button @click="toggleTheme" class="btn btn-light rounded-circle shadow-sm border hover-lift me-3 d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;" title="Змінити тему">
            <i class="bi fs-5" :class="isDarkMode ? 'bi-sun-fill text-warning' : 'bi-moon-stars-fill text-secondary'"></i>
          </button>
          <div class="dropdown">
            <a href="#" class="d-flex align-items-center text-dark text-decoration-none px-4 py-2 rounded-pill bg-light bg-opacity-75 border shadow-sm dropdown-toggle hover-lift" data-bs-toggle="dropdown">
              <i class="bi bi-person-check-fill fs-5 me-2 text-primary"></i>
              <span class="fw-bold small">{{ authState.user?.email }}</span>
            </a>
            <ul class="dropdown-menu dropdown-menu-end border-0 shadow-lg rounded-4 mt-2 p-2" style="min-width: 200px;">
              <li>
                <button class="dropdown-item text-danger fw-bold py-2 rounded-3 hover-lift" @click="authService.logout()">
                  <i class="bi bi-box-arrow-right me-2"></i> Вийти
                </button>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </nav>

    <div class="container-fluid px-4 px-xxl-5 py-4 flex-grow-1 d-flex overflow-hidden gap-4 position-relative z-1 animate__fadeIn" style="animation-delay: 0.1s;">

      <div class="glass-card flex-grow-1 d-flex flex-column overflow-hidden shadow-lg border-0" style="min-height: calc(100vh - 140px);">

        <div class="p-4 border-bottom d-flex align-items-center justify-content-between bg-white bg-opacity-50 backdrop-blur rounded-top-4 flex-wrap gap-3">
          <div class="d-flex align-items-center gap-3">
            <button v-if="!isRoot" @click="openFolder(currentFolderContent?.parentId)"
                    class="btn btn-white border shadow-sm px-4 py-2 rounded-pill fw-bold hover-lift d-flex align-items-center">
              <i class="bi bi-arrow-left me-2"></i> Назад
            </button>

            <nav aria-label="breadcrumb">
              <ol class="breadcrumb mb-0 py-1 fw-black fs-5 tracking-wider">
                <li class="breadcrumb-item"><a href="#" @click.prevent="openFolder(null)" class="text-decoration-none text-primary hover-glow">Моє сховище</a></li>
                <li v-if="!isRoot" class="breadcrumb-item active text-dark" aria-current="page">{{ currentFolderContent?.name }}</li>
              </ol>
            </nav>
          </div>

          <div class="d-flex gap-3">
            <div class="input-group bg-white rounded-pill shadow-sm border overflow-hidden" style="width: 280px;">
              <input v-model="newFolderName" @keyup.enter="createNewFolder" type="text" class="form-control border-0 shadow-none px-4 fw-medium text-dark bg-transparent" placeholder="Нова папка...">
              <button @click="createNewFolder" class="btn btn-light text-primary px-3 shadow-none hover-lift border-start"><i class="bi bi-folder-plus fs-5"></i></button>
            </div>
            <button class="btn magic-btn px-4 shadow-lg rounded-pill fw-bold d-flex align-items-center hover-lift" @click="triggerUpload">
              <i class="bi bi-cloud-arrow-up-fill me-2 fs-5"></i> <span class="tracking-wider">ЗАВАНТАЖИТИ</span>
            </button>
            <input type="file" ref="fileInput" class="d-none" @change="handleFileUpload">
          </div>
        </div>

        <div v-if="isLoading" class="flex-grow-1 d-flex flex-column align-items-center justify-content-center text-primary animate__fadeIn">
           <div class="spinner-border mb-3 border-3" style="width: 4rem; height: 4rem;"></div>
           <span class="fw-black tracking-wider text-uppercase small text-muted">Синхронізація...</span>
        </div>

        <div v-else class="flex-grow-1 overflow-auto custom-scrollbar p-3 p-md-4 bg-white bg-opacity-25 rounded-bottom-4">
           <div class="d-flex flex-column gap-3">

               <div v-for="folder in currentFolderContent?.folders" :key="folder.folderId" class="glass-panel p-3 d-flex align-items-center justify-content-between hover-lift transition-all cursor-pointer group-hover hover-bg" @click="editingId !== folder.folderId && openFolder(folder.folderId)">
                  <div class="d-flex align-items-center flex-grow-1">
                      <div class="p-2 bg-warning bg-opacity-10 rounded-circle me-3 shadow-sm d-flex align-items-center justify-content-center" style="width: 50px; height: 50px;">
                         <i class="bi bi-folder-fill text-warning fs-4" style="text-shadow: 0 0 15px rgba(255,193,7,0.5);"></i>
                      </div>
                      <div v-if="editingId === folder.folderId" class="input-group shadow-sm border border-primary rounded-pill overflow-hidden w-50" @click.stop>
                        <input v-model="renameValue" @keyup.enter="saveRename(folder.folderId)" @keyup.esc="editingId = null" class="form-control border-0 px-4 shadow-none fw-bold text-dark bg-white" autoFocus>
                        <button @click="saveRename(folder.folderId)" class="btn btn-success px-4 border-0"><i class="bi bi-check-lg"></i></button>
                        <button @click="editingId = null" class="btn btn-light text-danger px-3 border-0 border-start"><i class="bi bi-x-lg"></i></button>
                      </div>
                      <span v-else class="fw-bold text-dark tracking-tight fs-5">{{ folder.name }}</span>
                  </div>
                  <div class="d-flex gap-2 opacity-0-hover">
                     <button @click.stop="startRename(folder.folderId, folder.name)" class="btn btn-sm btn-light text-primary border rounded-circle shadow-sm hover-lift d-flex align-items-center justify-content-center" style="width: 44px; height: 44px;" title="Перейменувати"><i class="bi bi-pencil fs-5"></i></button>
                     <button @click.stop="deleteFolder(folder.folderId)" class="btn btn-sm btn-light text-danger border rounded-circle shadow-sm hover-lift d-flex align-items-center justify-content-center" style="width: 44px; height: 44px;" title="Видалити"><i class="bi bi-trash3-fill fs-5"></i></button>
                  </div>
               </div>

               <div v-for="file in currentFolderContent?.files" :key="file.fileId" class="glass-panel p-3 d-flex align-items-center justify-content-between hover-lift transition-all cursor-pointer group-hover hover-bg" :class="{'border-primary bg-primary bg-opacity-10': selectedFile?.fileId === file.fileId}" @click="viewFile(file)">
                  <div class="d-flex align-items-center flex-grow-1">
                      <div class="p-2 bg-primary bg-opacity-10 rounded-circle me-3 shadow-sm d-flex align-items-center justify-content-center" style="width: 50px; height: 50px;">
                         <i class="bi bi-file-earmark-code-fill text-primary fs-4 glow-text-primary"></i>
                      </div>
                      <span class="fw-bold text-dark tracking-tight fs-5">{{ file.name }}</span>
                  </div>
                  <div class="d-flex gap-2 opacity-0-hover">
                     <button @click.stop="downloadFile(file.fileId)" class="btn btn-sm btn-light text-primary border rounded-circle shadow-sm hover-lift d-flex align-items-center justify-content-center" style="width: 44px; height: 44px;" title="Завантажити"><i class="bi bi-cloud-arrow-down-fill fs-5"></i></button>
                     <button @click.stop="deleteFile(file.fileId)" class="btn btn-sm btn-light text-danger border rounded-circle shadow-sm hover-lift d-flex align-items-center justify-content-center" style="width: 44px; height: 44px;" title="Видалити"><i class="bi bi-trash3-fill fs-5"></i></button>
                  </div>
               </div>

               <div v-if="!currentFolderContent?.folders?.length && !currentFolderContent?.files?.length" class="h-100 d-flex flex-column align-items-center justify-content-center text-muted animate__fadeIn py-5 mt-5">
                  <div class="p-5 rounded-circle bg-white shadow-sm mb-4 border" style="background: rgba(255,255,255,0.5);">
                    <i class="bi bi-folder2-open display-1 text-primary glow-text-primary opacity-75"></i>
                  </div>
                  <h3 class="fw-black text-dark tracking-tight mb-2">Сховище порожнє</h3>
                  <p class="text-secondary fw-medium">Створіть нову папку або завантажте файл, щоб почати роботу.</p>
               </div>
           </div>
        </div>

      </div>

      <div v-if="selectedFile" class="glass-card shadow-lg d-flex flex-column animate__animated animate__slideInRight border-0 overflow-hidden" style="width: 500px; min-height: calc(100vh - 140px);">
        <div class="p-4 border-bottom d-flex justify-content-between align-items-center bg-white bg-opacity-75 rounded-top-4 backdrop-blur">
          <div class="d-flex align-items-center overflow-hidden w-100 pe-3">
            <div class="p-2 bg-primary bg-opacity-10 rounded-circle me-3">
               <i class="bi bi-file-earmark-text text-primary fs-5 glow-text-primary"></i>
            </div>
            <span class="fw-black text-truncate text-dark tracking-tight" style="font-size: 1.1rem;">{{ selectedFile.name }}</span>
          </div>
          <button @click="selectedFile = null" class="btn btn-light rounded-circle shadow-sm hover-lift d-flex align-items-center justify-content-center flex-shrink-0" style="width: 36px; height: 36px;">
             <i class="bi bi-x-lg text-secondary fw-bold"></i>
          </button>
        </div>

        <div class="flex-grow-1 p-3 bg-light bg-opacity-25 custom-scrollbar overflow-hidden">
           <div class="mac-window rounded-4 overflow-hidden border shadow-sm d-flex flex-column h-100 bg-dark">
              <div class="mac-header bg-black bg-opacity-25 d-flex align-items-center px-3 py-2 border-bottom border-secondary border-opacity-25">
                 <div class="d-flex gap-2">
                    <div class="rounded-circle bg-danger" style="width: 12px; height: 12px;"></div>
                    <div class="rounded-circle bg-warning" style="width: 12px; height: 12px;"></div>
                    <div class="rounded-circle bg-success" style="width: 12px; height: 12px;"></div>
                 </div>
                 <span class="ms-3 font-monospace text-secondary small fw-bold text-truncate">{{ selectedFile.name }}</span>
              </div>
              <div class="mac-body flex-grow-1 position-relative p-0 h-100">
                 <textarea readonly v-model="fileContent" class="form-control border-0 h-100 p-4 text-light bg-transparent font-monospace custom-scrollbar shadow-none" style="resize: none; font-size: 0.85rem; line-height: 1.6; white-space: pre; overflow-wrap: normal; overflow-x: auto;"></textarea>
              </div>
           </div>
        </div>

        <div class="p-4 border-top bg-white bg-opacity-50 rounded-bottom-4 d-grid backdrop-blur">
           <button @click="downloadFile(selectedFile.fileId)" class="btn magic-btn rounded-pill fw-bold py-3 shadow-lg hover-lift d-flex align-items-center justify-content-center">
             <i class="bi bi-cloud-arrow-down-fill me-2 fs-5"></i> <span class="tracking-wider">СКАЧАТИ ФАЙЛ</span>
           </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.cursor-pointer { cursor: pointer; }

.code-view {
  background: #1e1e1e;
  color: #d4d4d4;
  font-family: 'Fira Code', 'Consolas', monospace;
  font-size: 0.85rem;
  resize: none;
  line-height: 1.5;
}

.group-hover:hover .opacity-0-hover { opacity: 1 !important; }
.opacity-0-hover {
  opacity: 0;
  transition: opacity 0.2s ease-in-out;
}

.breadcrumb-item + .breadcrumb-item::before {
  content: "›";
  font-size: 1.2rem;
  line-height: 1;
  vertical-align: middle;
}

.table-hover tbody tr:hover {
  background-color: rgba(0, 123, 255, 0.02);
}

::-webkit-scrollbar { width: 8px; }
::-webkit-scrollbar-track { background: #f1f1f1; }
::-webkit-scrollbar-thumb { background: #ccc; border-radius: 4px; }
::-webkit-scrollbar-thumb:hover { background: #bbb; }
</style>
