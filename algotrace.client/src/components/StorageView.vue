<script setup lang="ts">
import { ref, onMounted, computed, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { authState, authService } from '@/services/auth.service';
import api from '@/services/api';
import { isDarkMode, toggleTheme } from '../composables/useTheme';
import { VueMonacoEditor } from '@guolao/vue-monaco-editor';

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

interface FSEntry {
  isFile: boolean;
  isDirectory: boolean;
  name: string;
}

interface FSFileEntry extends FSEntry {
  isFile: true;
  file: (resolve: (file: File) => void) => void;
}

interface FSDirectoryReader {
  readEntries: (resolve: (entries: FSEntry[]) => void) => void;
}

interface FSDirectoryEntry extends FSEntry {
  isDirectory: true;
  createReader: () => FSDirectoryReader;
}

const router = useRouter();

const currentFolderContent = ref<FolderContent | null>(null);
const selectedFile = ref<FileEntry | null>(null);
const fileContent = ref<string>('');
const isLoading = ref(false);
const isUploading = ref(false);
const uploadProgress = ref(0);
const isRoot = ref(true);

const isDragging = ref(false);
const dragCounter = ref(0);

const newFolderName = ref('');
const fileInput = ref<HTMLInputElement | null>(null);
const folderInput = ref<HTMLInputElement | null>(null);
const editingId = ref<string | null>(null);
const renameValue = ref('');

const breadcrumbs = ref<{id: string | null, name: string}[]>([{id: null, name: 'Моє сховище'}]);

const isPathExpanded = ref(false);

const visibleBreadcrumbs = computed(() => {
  const b = breadcrumbs.value;
  if (isPathExpanded.value || b.length <= 4) return b;
  return [
    b[0]!,
    { id: 'ellipsis', name: '...' },
    b[b.length - 3]!,
    b[b.length - 2]!,
    b[b.length - 1]!
  ];
});

const breadcrumbNav = ref<HTMLElement | null>(null);

const handleClickOutside = (event: MouseEvent) => {
  if (isPathExpanded.value && breadcrumbNav.value && !breadcrumbNav.value.contains(event.target as Node)) {
    isPathExpanded.value = false;
  }
};

const allowedExtensions = [
  '.cs', '.ts', '.tsx', '.js', '.jsx', '.html', '.css', '.scss', '.sass', '.less',
  '.java', '.kt', '.kts', '.py', '.c', '.cpp', '.h', '.hpp', '.php', '.rb',
  '.go', '.rs', '.swift', '.m', '.sql', '.json', '.xml', '.yaml', '.yml',
  '.sh', '.bash', '.bat', '.ps1'
];

const editorLanguage = computed(() => {
  if (!selectedFile.value) return 'plaintext';
  const name = selectedFile.value.name.toLowerCase();
  const ext = name.split('.').pop();
  
  const map: Record<string, string> = {
    'js': 'javascript', 'jsx': 'javascript',
    'ts': 'typescript', 'tsx': 'typescript',
    'cs': 'csharp', 'py': 'python',
    'cpp': 'cpp', 'c': 'c', 'h': 'cpp', 'hpp': 'cpp',
    'html': 'html', 'css': 'css', 'json': 'json',
    'java': 'java', 'go': 'go', 'rs': 'rust',
    'sql': 'sql', 'xml': 'xml', 'yaml': 'yaml', 'yml': 'yaml',
    'sh': 'shell', 'bash': 'shell'
  };
  
  return ext && map[ext] ? map[ext] : 'plaintext';
});

const filePanelWidth = ref(500);
const isResizing = ref(false);
const startX = ref(0);
const startWidth = ref(0);

const startResize = (e: MouseEvent) => {
  isResizing.value = true;
  startX.value = e.clientX;
  startWidth.value = filePanelWidth.value;
  document.addEventListener('mousemove', onResize);
  document.addEventListener('mouseup', stopResize);
  document.body.style.userSelect = 'none';
};

const onResize = (e: MouseEvent) => {
  if (!isResizing.value) return;
  const deltaX = startX.value - e.clientX;
  let newWidth = startWidth.value + deltaX;
  
  if (newWidth < 300) newWidth = 300;
  if (newWidth > window.innerWidth - 400) newWidth = window.innerWidth - 400;
  
  filePanelWidth.value = newWidth;
};

const stopResize = () => {
  isResizing.value = false;
  document.removeEventListener('mousemove', onResize);
  document.removeEventListener('mouseup', stopResize);
  document.body.style.userSelect = '';
};

const isValidFile = (file: File) => {
  const name = file.name.toLowerCase();
  return allowedExtensions.some(ext => name.endsWith(ext));
};

const openFolder = async (folderId: string | null = null) => {
  isLoading.value = true;
  isRoot.value = folderId === null;
  try {
    const url = folderId ? `/api/directory/folder/${folderId}` : '/api/directory/folder';
    const res = await api.get<FolderContent>(url);
    currentFolderContent.value = res.data;
    selectedFile.value = null;
  } catch (err) {
    console.error(err);
    if (!isRoot.value) alert("Помилка доступу до папки");
  } finally {
    isLoading.value = false;
  }
};

const enterFolder = (id: string, name: string) => {
  breadcrumbs.value.push({ id, name });
  isPathExpanded.value = false;
  openFolder(id);
};

const navigateToCrumb = (crumb: {id: string | null, name: string}) => {
  const index = breadcrumbs.value.findIndex(c => c.id === crumb.id);
  if (index !== -1) {
    breadcrumbs.value = breadcrumbs.value.slice(0, index + 1);
    isPathExpanded.value = false;
    openFolder(crumb.id);
  }
};

const goBack = () => {
  if (breadcrumbs.value.length > 1) {
    breadcrumbs.value.pop();
    isPathExpanded.value = false;
    openFolder(breadcrumbs.value[breadcrumbs.value.length - 1]!.id);
  }
};

const triggerFileUpload = () => fileInput.value?.click();
const triggerFolderUpload = () => folderInput.value?.click();

const uploadFiles = async (
  files: FileList | File[], 
  targetFolderId?: string | null, 
  skipRefresh: boolean = false
) => {
  const validFiles = Array.from(files).filter(isValidFile);
  if (validFiles.length === 0) return;

  const formData = new FormData();
  validFiles.forEach(file => {
    formData.append('files', file, file.name); 
  });

  const uploadId = targetFolderId !== undefined ? targetFolderId : (currentFolderContent.value?.folderId || null);
  const query = uploadId ? `?folderId=${uploadId}` : '';

  try {
    if (!skipRefresh) {
      isUploading.value = true;
      uploadProgress.value = 0;
    }
    
    await api.post(`/api/directory/file/upload${query}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total && !skipRefresh) {
          uploadProgress.value = Math.round((progressEvent.loaded * 100) / progressEvent.total);
        }
      }
    });

    if (!skipRefresh) await openFolder(currentFolderContent.value?.folderId || null);
  } catch (err) {
    console.error(err);
    if (!skipRefresh) alert("Помилка завантаження файлів");
  } finally {
    if (!skipRefresh) {
      isUploading.value = false;
      uploadProgress.value = 0;
    }
  }
};

const handleFileUpload = (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (target.files) {
    uploadFiles(target.files);
    target.value = ''; 
  }
};

const handleFolderUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (!target.files || target.files.length === 0) return;

  isUploading.value = true;
  uploadProgress.value = 100;

  const files = Array.from(target.files).filter(isValidFile);
  if (files.length === 0) {
    alert("У вибраній папці немає файлів з дозволеними розширеннями коду.");
    isUploading.value = false;
    target.value = '';
    return;
  }

  const folderMap = new Map<string, string>();
  const rootParentId = currentFolderContent.value?.folderId || null;

  for (const file of files) {
    const pathParts = file.webkitRelativePath.split('/');
    pathParts.pop(); 
    
    let currentParent = rootParentId;
    let currentPath = '';

    for (const part of pathParts) {
      currentPath = currentPath ? `${currentPath}/${part}` : part;
      
      if (!folderMap.has(currentPath)) {
        try {
          const res = await api.post('/api/directory/folder', {
            name: part,
            parentId: currentParent
          });
          const newId = res.data.folderId || res.data.FolderId || res.data.id;
          folderMap.set(currentPath, newId);
          currentParent = newId;
        } catch (e) {
          console.error(e);
          break;
        }
      } else {
        currentParent = folderMap.get(currentPath)!;
      }
    }

    await uploadFiles([file], currentParent, true);
  }

  isUploading.value = false;
  target.value = '';
  await openFolder(rootParentId);
};

const getFileFromEntry = (entry: FSFileEntry): Promise<File> => {
  return new Promise((resolve) => entry.file(resolve));
};

const readDirectoryEntries = async (dirReader: FSDirectoryReader): Promise<FSEntry[]> => {
  const entries: FSEntry[] = [];
  let readEntries = await new Promise<FSEntry[]>((resolve) => dirReader.readEntries(resolve));
  while (readEntries.length > 0) {
    entries.push(...readEntries);
    readEntries = await new Promise<FSEntry[]>((resolve) => dirReader.readEntries(resolve));
  }
  return entries;
};

const processEntryRecursive = async (entry: FSEntry, targetFolderId: string | null) => {
  if (entry.isFile) {
    const file = await getFileFromEntry(entry as FSFileEntry);
    if (isValidFile(file)) {
      await uploadFiles([file], targetFolderId, true);
    }
  } else if (entry.isDirectory) {
    let newFolderId = targetFolderId;
    
    try {
      const res = await api.post('/api/directory/folder', {
        name: entry.name,
        parentId: targetFolderId
      });
      newFolderId = res.data.folderId || res.data.FolderId || res.data.id;
    } catch (e) {
      console.error(e);
      return; 
    }

    const dirReader = (entry as FSDirectoryEntry).createReader();
    const entries = await readDirectoryEntries(dirReader);
    const filesToUpload: File[] = [];
    
    for (const childEntry of entries) {
      if (childEntry.isFile) {
        const file = await getFileFromEntry(childEntry as FSFileEntry);
        if (isValidFile(file)) filesToUpload.push(file);
      } else if (childEntry.isDirectory) {
        await processEntryRecursive(childEntry, newFolderId);
      }
    }

    if (filesToUpload.length > 0) {
      await uploadFiles(filesToUpload, newFolderId, true);
    }
  }
};

const handleDragEnter = (e: DragEvent) => {
  e.preventDefault();
  dragCounter.value++;
  isDragging.value = true;
};

const handleDragLeave = (e: DragEvent) => {
  e.preventDefault();
  dragCounter.value--;
  if (dragCounter.value === 0) isDragging.value = false;
};

const handleDragOver = (e: DragEvent) => e.preventDefault();

const handleDrop = async (e: DragEvent) => {
  e.preventDefault();
  dragCounter.value = 0;
  isDragging.value = false;
  
  if (!e.dataTransfer?.items) return;

  isUploading.value = true;
  uploadProgress.value = 100;

  const items = Array.from(e.dataTransfer.items);
  const currentParentId = currentFolderContent.value?.folderId || null;
  const rootFiles: File[] = [];

  for (const item of items) {
    const entry = item.webkitGetAsEntry() as FSEntry | null;
    if (!entry) continue;

    if (entry.isFile) {
      const file = await getFileFromEntry(entry as FSFileEntry);
      if (isValidFile(file)) rootFiles.push(file);
    } else if (entry.isDirectory) {
      await processEntryRecursive(entry, currentParentId);
    }
  }

  if (rootFiles.length > 0) {
    await uploadFiles(rootFiles, currentParentId, true);
  }

  isUploading.value = false;
  uploadProgress.value = 0;
  await openFolder(currentFolderContent.value?.folderId || null);
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
    console.error(err);
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
    console.error(err);
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
    console.error(err);
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
    console.error(err);
    alert("Помилка перейменування");
  }
};

const deleteFolder = async (id: string) => {
  if (!confirm('Видалити папку та все вкладене?')) return;
  try {
    await api.delete(`/api/directory/folder/${id}`);
    await openFolder(currentFolderContent.value?.parentId || null);
  } catch (err) {
    console.error(err);
    alert("Не вдалося видалити папку");
  }
};

onMounted(() => {
  if (!authState.isAuthenticated) {
    router.push('/auth');
    return;
  }
  openFolder(null);
  
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
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

      <div class="glass-card flex-grow-1 d-flex flex-column overflow-hidden shadow-lg border-0" style="min-height: calc(100vh - 140px); min-width: 300px;">

        <div class="p-4 border-bottom d-flex align-items-center justify-content-between bg-white bg-opacity-50 backdrop-blur rounded-top-4 flex-wrap gap-3">
          <div class="d-flex align-items-center gap-3 flex-grow-1" style="min-width: 0;">
            <button v-if="!isRoot" @click="goBack"
                    class="btn btn-white border shadow-sm px-4 py-2 rounded-pill fw-bold hover-lift d-flex align-items-center flex-shrink-0">
              <i class="bi bi-arrow-left me-2"></i> Назад
            </button>

            <nav ref="breadcrumbNav" aria-label="breadcrumb" class="overflow-x-auto subtle-scrollbar" style="min-width: 0;">
              <ol class="breadcrumb mb-0 py-1 fw-black fs-5 tracking-wider flex-nowrap pb-1">
                <li v-for="(crumb, index) in visibleBreadcrumbs" :key="crumb!.id || 'root'"
                    class="breadcrumb-item text-nowrap flex-shrink-0"
                    :class="{ active: index === visibleBreadcrumbs.length - 1 }">
                 <a v-if="index < visibleBreadcrumbs.length - 1"
                    href="#"
                    @click.prevent.stop="crumb!.id === 'ellipsis' ? isPathExpanded = true : navigateToCrumb(crumb!)"
                    class="text-decoration-none text-primary hover-glow"
                    :class="{ 'text-muted': crumb!.id === 'ellipsis' }">
                    {{ crumb!.name }}
                  </a>
                  <span v-else class="text-dark">{{ crumb!.name }}</span>
                </li>
              </ol>
            </nav>
          </div>

          <div class="d-flex gap-3">
            <div class="input-group bg-white rounded-pill shadow-sm border overflow-hidden" style="width: 280px;">
              <input v-model="newFolderName" @keyup.enter="createNewFolder" type="text" class="form-control border-0 shadow-none px-4 fw-medium text-dark bg-transparent" placeholder="Нова папка...">
              <button @click="createNewFolder" class="btn btn-light text-primary px-3 shadow-none hover-lift border-start"><i class="bi bi-folder-plus fs-5"></i></button>
            </div>
            
            <div class="dropdown">
              <button class="btn magic-btn px-4 shadow-lg rounded-pill fw-bold d-flex align-items-center hover-lift dropdown-toggle" data-bs-toggle="dropdown" :disabled="isUploading">
                <i class="bi bi-cloud-arrow-up-fill me-2 fs-5"></i> <span class="tracking-wider">ЗАВАНТАЖИТИ</span>
              </button>
              <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0 rounded-4 mt-2 p-2">
                <li>
                  <a class="dropdown-item fw-bold py-2 rounded-3 hover-lift d-flex align-items-center" href="#" @click.prevent="triggerFileUpload">
                    <i class="bi bi-file-earmark-plus fs-5 me-3 text-primary"></i> Завантажити файли
                  </a>
                </li>
                <li>
                  <a class="dropdown-item fw-bold py-2 rounded-3 hover-lift d-flex align-items-center" href="#" @click.prevent="triggerFolderUpload">
                    <i class="bi bi-folder-plus fs-5 me-3 text-warning"></i> Завантажити папку
                  </a>
                </li>
              </ul>
            </div>
            
            <input type="file" ref="fileInput" class="d-none" @change="handleFileUpload" multiple>
            <input type="file" ref="folderInput" class="d-none" @change="handleFolderUpload" webkitdirectory directory multiple>
          </div>
        </div>

        <div v-if="isLoading || isUploading" class="flex-grow-1 d-flex flex-column align-items-center justify-content-center text-primary animate__fadeIn p-4">
           <template v-if="isLoading && !isUploading">
             <div class="spinner-border mb-3 border-3" style="width: 4rem; height: 4rem;"></div>
             <span class="fw-black tracking-wider text-uppercase small text-muted">Синхронізація...</span>
           </template>
           
           <div v-if="isUploading" class="w-100 d-flex flex-column align-items-center" style="max-width: 400px;">
             <div class="p-4 bg-primary bg-opacity-10 rounded-circle mb-4 shadow-sm">
                <i class="bi bi-cloud-arrow-up-fill text-primary" style="font-size: 3rem;"></i>
             </div>
             <h4 class="fw-bold mb-4 text-dark text-center">Побудова структури та завантаження...</h4>
             <div class="progress w-100 shadow-sm border" style="height: 25px; border-radius: 12px; background-color: rgba(0,0,0,0.05);">
               <div class="progress-bar progress-bar-striped progress-bar-animated bg-primary fw-bold"
                    role="progressbar"
                    :style="{ width: uploadProgress === 100 ? '100%' : uploadProgress + '%' }"
                    aria-valuemin="0"
                    aria-valuemax="100">
                 {{ uploadProgress === 100 ? 'Зачекайте...' : uploadProgress + '%' }}
               </div>
             </div>
             <p class="text-muted small mt-3 fw-medium text-center">Файли перевіряються та розміщуються по папках. Це може зайняти певний час.</p>
           </div>
        </div>

        <div v-else 
             class="flex-grow-1 overflow-auto custom-scrollbar p-3 p-md-4 bg-white bg-opacity-25 rounded-bottom-4 position-relative transition-all"
             @dragenter="handleDragEnter"
             @dragleave="handleDragLeave"
             @dragover="handleDragOver"
             @drop="handleDrop">
             
           <div v-if="isDragging" class="position-absolute top-0 start-0 w-100 h-100 d-flex flex-column align-items-center justify-content-center rounded-bottom-4 animate__animated animate__fadeIn animate__faster" 
                style="background: rgba(13, 110, 253, 0.08); backdrop-filter: blur(4px); z-index: 10; border: 3px dashed rgba(13, 110, 253, 0.5); pointer-events: none;">
               <div class="p-4 bg-white rounded-circle shadow-lg mb-3">
                 <i class="bi bi-cloud-arrow-up-fill text-primary" style="font-size: 3rem;"></i>
               </div>
               <h4 class="fw-black text-primary tracking-wider" style="text-shadow: 0 2px 4px rgba(255,255,255,0.8);">Відпустіть файли або папки для завантаження</h4>
           </div>

           <div class="d-flex flex-column gap-3" style="min-width: max-content; padding-right: 15px;">

              <div v-for="folder in currentFolderContent?.folders" :key="folder.folderId" class="glass-panel p-3 d-flex align-items-center justify-content-between hover-lift transition-all cursor-pointer group-hover hover-bg" @click="editingId !== folder.folderId && enterFolder(folder.folderId, folder.name)">
                  <div class="d-flex align-items-center flex-grow-1 me-4"> <div class="p-2 bg-warning bg-opacity-10 rounded-circle me-3 shadow-sm d-flex align-items-center justify-content-center flex-shrink-0" style="width: 50px; height: 50px;">
                        <i class="bi bi-folder-fill text-warning fs-4" style="text-shadow: 0 0 15px rgba(255,193,7,0.5);"></i>
                      </div>
                      <div v-if="editingId === folder.folderId" class="input-group shadow-sm border border-primary rounded-pill overflow-hidden w-50" @click.stop>
                        <input v-model="renameValue" @keyup.enter="saveRename(folder.folderId)" @keyup.esc="editingId = null" class="form-control border-0 px-4 shadow-none fw-bold text-dark bg-white" autoFocus>
                        <button @click="saveRename(folder.folderId)" class="btn btn-success px-4 border-0"><i class="bi bi-check-lg"></i></button>
                        <button @click="editingId = null" class="btn btn-light text-danger px-3 border-0 border-start"><i class="bi bi-x-lg"></i></button>
                      </div>
                      <span v-else class="fw-bold text-dark tracking-tight fs-5 text-nowrap">{{ folder.name }}</span>
                  </div>
                  <div class="d-flex gap-2 opacity-0-hover flex-shrink-0">
                    <button @click.stop="startRename(folder.folderId, folder.name)" class="btn btn-sm btn-light text-primary border rounded-circle shadow-sm hover-lift d-flex align-items-center justify-content-center" style="width: 44px; height: 44px;" title="Перейменувати"><i class="bi bi-pencil fs-5"></i></button>
                    <button @click.stop="deleteFolder(folder.folderId)" class="btn btn-sm btn-light text-danger border rounded-circle shadow-sm hover-lift d-flex align-items-center justify-content-center" style="width: 44px; height: 44px;" title="Видалити"><i class="bi bi-trash3-fill fs-5"></i></button>
                  </div>
              </div>

              <div v-for="file in currentFolderContent?.files" :key="file.fileId" class="glass-panel p-3 d-flex align-items-center justify-content-between hover-lift transition-all cursor-pointer group-hover hover-bg" :class="{'border-primary bg-primary bg-opacity-10': selectedFile?.fileId === file.fileId}" @click="viewFile(file)">
                  <div class="d-flex align-items-center flex-grow-1 me-4"> <div class="p-2 bg-primary bg-opacity-10 rounded-circle me-3 shadow-sm d-flex align-items-center justify-content-center flex-shrink-0" style="width: 50px; height: 50px;">
                        <i class="bi bi-file-earmark-code-fill text-primary fs-4 glow-text-primary"></i>
                      </div>
                      <span class="fw-bold text-dark tracking-tight fs-5 text-nowrap">{{ file.name }}</span>
                  </div>
                  <div class="d-flex gap-2 opacity-0-hover flex-shrink-0">
                    <button @click.stop="downloadFile(file.fileId)" class="btn btn-sm btn-light text-primary border rounded-circle shadow-sm hover-lift d-flex align-items-center justify-content-center" style="width: 44px; height: 44px;" title="Завантажити"><i class="bi bi-cloud-arrow-down-fill fs-5"></i></button>
                    <button @click.stop="deleteFile(file.fileId)" class="btn btn-sm btn-light text-danger border rounded-circle shadow-sm hover-lift d-flex align-items-center justify-content-center" style="width: 44px; height: 44px;" title="Видалити"><i class="bi bi-trash3-fill fs-5"></i></button>
                  </div>
              </div>

              <div v-if="!currentFolderContent?.folders?.length && !currentFolderContent?.files?.length && !isDragging" class="h-100 d-flex flex-column align-items-center justify-content-center text-muted animate__fadeIn py-5 mt-5">
                  </div>
            </div>
        </div>

      </div>

      <div v-if="selectedFile" class="glass-card shadow-lg d-flex flex-column animate__animated animate__slideInRight border-0 overflow-hidden position-relative flex-shrink-0" :style="{ width: filePanelWidth + 'px', minWidth: '300px', minHeight: 'calc(100vh - 140px)' }">
        <div class="resize-handle" :class="{ 'active': isResizing }" @mousedown="startResize"></div>
        
        <div class="p-3 border-bottom d-flex justify-content-between align-items-center bg-white bg-opacity-75 rounded-top-4 backdrop-blur">
          <div class="d-flex align-items-center overflow-hidden pe-3">
            <div class="p-2 bg-primary bg-opacity-10 rounded-circle me-3 flex-shrink-0">
              <i class="bi bi-file-earmark-text text-primary fs-5 glow-text-primary"></i>
            </div>
            <span class="fw-black text-truncate text-dark tracking-tight" style="font-size: 1.1rem;">{{ selectedFile.name }}</span>
          </div>
          
          <div class="d-flex align-items-center gap-2 flex-shrink-0">
            <button @click="downloadFile(selectedFile.fileId)" class="btn btn-sm btn-primary rounded-pill fw-bold px-3 shadow-sm hover-lift d-flex align-items-center">
              <i class="bi bi-cloud-arrow-down-fill me-2"></i> Скачати
            </button>
            <button @click="selectedFile = null" class="btn btn-light rounded-circle shadow-sm hover-lift d-flex align-items-center justify-content-center" style="width: 36px; height: 36px;">
              <i class="bi bi-x-lg text-secondary fw-bold"></i>
            </button>
          </div>
        </div>

        <div class="mac-body flex-grow-1 position-relative p-0 h-100 bg-white">
          <VueMonacoEditor
            v-model:value="fileContent"
            :theme="isDarkMode ? 'vs-dark' : 'vs'"
            :language="editorLanguage"
            :options="{ 
              readOnly: true, 
              minimap: { enabled: false }, 
              scrollBeyondLastLine: false, 
              fontSize: 13, 
              smoothScrolling: true,
              padding: { top: 16 }
            }"
          />
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

.resize-handle {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 6px;
  cursor: ew-resize;
  z-index: 10;
  transition: background-color 0.2s;
}

.resize-handle:hover,
.resize-handle.active { background-color: rgba(13, 110, 253, 0.5); }

::-webkit-scrollbar { width: 8px; }
::-webkit-scrollbar-track { background: #f1f1f1; }
::-webkit-scrollbar-thumb { background: #ccc; border-radius: 4px; }
::-webkit-scrollbar-thumb:hover { background: #bbb; }

.subtle-scrollbar::-webkit-scrollbar { height: 4px; }
.subtle-scrollbar::-webkit-scrollbar-track { background: transparent; }
.subtle-scrollbar::-webkit-scrollbar-thumb { background: rgba(13, 110, 253, 0.3); border-radius: 4px; }
.subtle-scrollbar:hover::-webkit-scrollbar-thumb { background: rgba(13, 110, 253, 0.6); }
</style>