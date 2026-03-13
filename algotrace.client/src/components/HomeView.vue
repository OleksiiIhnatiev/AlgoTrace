<script setup lang="ts">
import { ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { authService, authState } from '../services/auth.service';
import { analysisService, analysisState, type AnalysisPayload, type FileContent } from '../services/analysis.service';
import api from '../services/api';
import FileTreeItem from '@/components/FileTreeItem.vue';

interface Node {
  id: number;
  name: string;
  type: 'folder' | 'file';
}

const router = useRouter();

const file1 = ref<File | null>(null);
const file2 = ref<File | null>(null);
const isLoading = ref(false);
const isDragging1 = ref(false);
const isDragging2 = ref(false);

const inputMode = ref<'file' | 'text' | 'storage'>('file');
const storageLeftMode = ref<'file' | 'text'>('file');

const code1 = ref('');
const code2 = ref('');

// Стан для дерева сховища
const rootNodes = ref<Node[]>([]);
const selectedStorageIds = ref<number[]>([]);
const isStorageLoading = ref(false);

const selectedLanguage = ref('python');

// --- Конфигурация категорий и методов ---
const analysisConfig = [
  {
    id: 'text',
    name: 'Text Analysis',
    methods: [
      { value: 'levenshtein', label: 'Levenshtein Distance' },
      { value: 'line_matching', label: 'Line Matching' },
      { value: 'rabin_karp', label: 'Rabin-Karp' },
      { value: 'ngram_search', label: 'N-gram Search' }
    ]
  },
  {
    id: 'token',
    name: 'Token Analysis',
    methods: [
      { value: 'jaccard_token', label: 'Jaccard Token' },
      { value: 'winnowing', label: 'Winnowing' }
    ]
  },
  {
    id: 'tree',
    name: 'Tree (AST) Analysis',
    methods: [
      { value: 'ast_hashing', label: 'AST Hashing' },
      { value: 'ast_compare', label: 'AST Compare' },
      { value: 'subtree_isomorphism', label: 'Subtree Isomorphism' }
    ]
  },
  {
    id: 'graph',
    name: 'Graph (CFG/PDG)',
    methods: [
      { value: 'cfg', label: 'Control Flow Graph (CFG)' },
      { value: 'pdg', label: 'Program Dependence Graph (PDG)' },
      { value: 'subgraph_isomorphism', label: 'Subgraph Isomorphism' }
    ]
  },
  {
    id: 'metric',
    name: 'Software Metrics',
    methods: [
      { value: 'halstead', label: 'Halstead Metrics' },
      { value: 'mccabe', label: 'McCabe Complexity' }
    ]
  }
];

const selectedMethods = ref<Record<string, string[]>>({});

// --- Завантаження сховища ---
const loadStorageTree = async () => {
  if (!authState.isAuthenticated) return;
  isStorageLoading.value = true;
  try {
    const res = await api.get('/api/directory/folder');
    const folders = res.data.folders.map((f: { folderId: number; name: string }) => ({ id: f.folderId, name: f.name, type: 'folder' }));
    const files = res.data.files.map((f: { fileId: number; name: string }) => ({ id: f.fileId, name: f.name, type: 'file' }));
    rootNodes.value = [...folders, ...files];
  } catch (err) {
    console.error("Error loading storage tree:", err);
  } finally {
    isStorageLoading.value = false;
  }
};

watch(inputMode, (newVal) => {
  if (newVal === 'storage' && rootNodes.value.length === 0) {
    loadStorageTree();
  }
});

// --- Рекурсивне виділення папок ---
const checkNodeAndChildren = async (id: number) => {
  if (!selectedStorageIds.value.includes(id)) selectedStorageIds.value.push(id);
  try {
    const res = await api.get(`/api/directory/folder/${id}`);
    for (const f of res.data.files) {
      if (!selectedStorageIds.value.includes(f.fileId)) selectedStorageIds.value.push(f.fileId);
    }
    for (const f of res.data.folders) {
      await checkNodeAndChildren(f.folderId);
    }
  } catch {
    // Якщо це файл, API поверне помилку, ігноруємо
  }
};

const uncheckNodeAndChildren = async (id: number) => {
  selectedStorageIds.value = selectedStorageIds.value.filter(selId => selId !== id);
  try {
    const res = await api.get(`/api/directory/folder/${id}`);
    for (const f of res.data.files) {
      selectedStorageIds.value = selectedStorageIds.value.filter(selId => selId !== f.fileId);
    }
    for (const f of res.data.folders) {
      await uncheckNodeAndChildren(f.folderId);
    }
  } catch {
    // Якщо це файл, ігноруємо
  }
};

const toggleStorageSelection = async (id: number) => {
  const isSelected = selectedStorageIds.value.includes(id);
  if (isSelected) {
    await uncheckNodeAndChildren(id);
  } else {
    await checkNodeAndChildren(id);
  }
};

// --- Обробка файлів ---
const handleDrop = (event: DragEvent, fileNumber: 1 | 2) => {
  const files = event.dataTransfer?.files;
  if (files && files.length > 0) {
    validateAndSetFile(files[0]!, fileNumber);
  }
  if (fileNumber === 1) isDragging1.value = false;
  else isDragging2.value = false;
};

const handleFileSelect = (event: Event, fileNumber: 1 | 2) => {
  const input = event.target as HTMLInputElement;
  if (input.files && input.files.length > 0) {
    validateAndSetFile(input.files[0]!, fileNumber);
  }
};

const validateAndSetFile = (file: File, fileNumber: 1 | 2) => {
  const allowedExtensions = ['.cs', '.py', '.js', '.ts', '.java', '.cpp', '.h', '.html', '.css', '.php', '.rb', '.go'];
  const fileName = file.name.toLowerCase();
  const isValid = allowedExtensions.some(ext => fileName.endsWith(ext));

  if (isValid) {
    if (fileNumber === 1) file1.value = file;
    else file2.value = file;
  } else {
    alert('Будь ласка, виберіть файл із кодом (наприклад: .cs, .py, .js)');
  }
};

const removeFile = (fileNumber: 1 | 2) => {
  if (fileNumber === 1) file1.value = null;
  else file2.value = null;
};

const toggleMethod = (categoryId: string, method: string) => {
  if (!selectedMethods.value[categoryId]) {
    selectedMethods.value[categoryId] = [];
  }
  const idx = selectedMethods.value[categoryId].indexOf(method);
  if (idx === -1) selectedMethods.value[categoryId].push(method);
  else selectedMethods.value[categoryId].splice(idx, 1);

  if (selectedMethods.value[categoryId].length === 0) {
    delete selectedMethods.value[categoryId];
  }
};

const startComparison = async () => {
  const hasSelectedMethods = Object.keys(selectedMethods.value).length > 0;

  if (inputMode.value === 'file' && (!file1.value || !file2.value)) {
    alert('Будь ласка, завантажте обидва файли для порівняння.');
    return;
  }
  if (inputMode.value === 'text' && (!code1.value.trim() || !code2.value.trim())) {
    alert('Будь ласка, вставте код у обидва поля для порівняння.');
    return;
  }
  if (inputMode.value === 'storage') {
    if (storageLeftMode.value === 'file' && !file1.value) {
      alert('Будь ласка, завантажте файл для лівої панелі.'); return;
    }
    if (storageLeftMode.value === 'text' && !code1.value.trim()) {
      alert('Будь ласка, вставте код для лівої панелі.'); return;
    }
    if (selectedStorageIds.value.length === 0) {
      alert('Будь ласка, оберіть файли зі сховища.'); return;
    }
  }
  if (!hasSelectedMethods) {
    alert('Будь ласка, оберіть хоча б один метод аналізу.');
    return;
  }

  isLoading.value = true;
  analysisState.currentReport = null;

  try {
    let submissionAData: { files: FileContent[] } | undefined;
    let submissionBData: { files: FileContent[] } | undefined;
    let language = 'python';

    // 1. Подготовка данных в зависимости от режима
    if (inputMode.value === 'file') {
      const [res1, res2] = await Promise.all([
        analysisService.uploadFile(file1.value!),
        analysisService.uploadFile(file2.value!)
      ]);
      language = res1.data.language || 'python';
      submissionAData = res1.data.submission;
      submissionBData = res2.data.submission;

    } else if (inputMode.value === 'text') {
      language = selectedLanguage.value;
      submissionAData = { files: [{ filename: "submission.txt", content: code1.value }] };
      submissionBData = { files: [{ filename: "reference.txt", content: code2.value }] };

    } else if (inputMode.value === 'storage') {
      // Ліва панель (storageLeftMode)
      if (storageLeftMode.value === 'file') {
        const res1 = await analysisService.uploadFile(file1.value!);
        language = res1.data.language || 'python';
        submissionAData = res1.data.submission;
      } else {
        language = selectedLanguage.value;
        submissionAData = { files: [{ filename: "submission.txt", content: code1.value }] };
      }

      // Права панель (сховище)
      const storageFiles: FileContent[] = [];
      for (const id of selectedStorageIds.value) {
        try {
          // Завантажуємо лише файли (спроба завантажити папку викличе помилку та перейде в catch)
          const res = await api.get(`/api/directory/file/download/${id}`, { responseType: 'text' });
          storageFiles.push({ filename: `storage_file_${id}.txt`, content: res.data });
        } catch {
          // Це може бути ID папки, ігноруємо
        }
      }
      if (storageFiles.length === 0) {
        alert('Не вдалося завантажити файли зі сховища.');
        isLoading.value = false;
        return;
      }
      submissionBData = { files: storageFiles };
    }

    // 2. Формування execute_categories
    const executeCategories: Record<string, string[]> = {};
    const categoryMap: Record<string, string> = {
      'text': 'text_based',
      'token': 'token_based',
      'tree': 'tree_based',
      'graph': 'graph_based',
      'metric': 'metrics_based'
    };

    for (const [catId, methods] of Object.entries(selectedMethods.value)) {
      if (methods && methods.length > 0) {
        const apiCategoryKey = categoryMap[catId];
        if (apiCategoryKey) {
          executeCategories[apiCategoryKey] = methods;
        }
      }
    }

    // 3. Відправка запиту
    const payload: AnalysisPayload = {
      language: language,
      submission_a: submissionAData!,
      submission_b: submissionBData!,
      analysis_config: {
        parameters: { ignore_comments: true, ignore_whitespace: true },
        execute_categories: executeCategories
      }
    };

    const response = await analysisService.analyze('/api/analysis/unified', payload);

    if (response.data) {
      analysisState.currentReport = response.data as Record<string, unknown>;
      router.push('/analyzer');
    }
  } catch (e) {
    console.error('Analysis failed', e);
    alert('Помилка під час аналізу. Перевірте консоль або спробуйте пізніше.');
  } finally {
    isLoading.value = false;
  }
};

const resendEmail = async () => {
  if (authState.user?.email) {
    try {
      await authService.resendConfirmationEmail(authState.user.email);
      alert('Лист для підтвердження було відправлено повторно!');
    } catch (e) {
      console.error(e);
      alert('Помилка відправки листа.');
    }
  }
};
</script>

<template>
  <div class="min-vh-100 bg-body-tertiary">
    <nav class="navbar navbar-expand-lg navbar-light bg-white sticky-top shadow-sm">
      <div class="container">
        <a class="navbar-brand fw-bold d-flex align-items-center text-primary" href="#">
          <div class="bg-primary bg-gradient text-white rounded-3 p-2 me-2 d-flex align-items-center justify-content-center shadow-sm" style="width: 40px; height: 40px;">
            <i class="bi bi-layers-half fs-5"></i>
          </div>
          <span>AlgoTrace</span>
        </a>

        <div class="d-flex align-items-center">
          <div v-if="!authState.isAuthenticated" class="dropdown">
            <router-link to="/auth" class="d-flex align-items-center text-dark text-decoration-none px-3 py-2 rounded-pill hover-bg-light">
              <i class="bi bi-person-circle fs-4 me-2 text-secondary"></i>
              <span class="fw-medium">Увійти</span>
            </router-link>
          </div>

          <div v-else class="dropdown">
            <a href="#" class="d-flex align-items-center text-dark text-decoration-none px-3 py-2 rounded-pill bg-light dropdown-toggle" data-bs-toggle="dropdown">
              <i class="bi bi-person-check-fill fs-4 me-2 text-primary"></i>
              <span class="fw-bold small">{{ authState.user?.email }}</span>
            </a>
            <ul class="dropdown-menu dropdown-menu-end border-0 shadow-lg rounded-3 mt-2">
              <li>
                <router-link to="/storage" class="dropdown-item fw-bold py-2">
                  <i class="bi bi-folder2-open me-2 text-primary"></i> Мої файли
                </router-link>
              </li>
              <li><hr class="dropdown-divider"></li>
              <li>
                <button class="dropdown-item text-danger fw-bold py-2" @click="authService.logout()">
                  <i class="bi bi-box-arrow-right me-2"></i> Вийти
                </button>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </nav>

    <div v-if="authState.isAuthenticated && !authState.user?.isEmailConfirmed" class="alert alert-warning text-center rounded-0 border-0 mb-0 shadow-sm" style="font-size: 0.9rem;">
      <i class="bi bi-exclamation-triangle-fill me-2"></i>
      Ваш email не підтверджено. Деякі функції можуть бути обмежені. <a href="#" @click.prevent="resendEmail" class="alert-link fw-bold">Надіслати лист повторно</a>
    </div>

    <div class="container py-5">
      <div class="text-center mb-5">
        <h1 class="display-4 fw-bolder text-dark mb-3">
          Порівняння коду <span class="text-primary">нового рівня</span>
        </h1>
        <p class="lead text-secondary mx-auto col-lg-7">
          Завантажте файли, налаштуйте глибину аналізу та отримайте миттєвий результат.
        </p>
      </div>

      <div class="d-flex justify-content-center align-items-center gap-3 mb-4" style="min-height: 38px;">
        <div class="bg-white p-1 rounded-pill shadow-sm border">
          <button
            class="btn btn-sm rounded-pill px-4 fw-bold"
            :class="inputMode === 'file' ? 'btn-primary' : 'btn-light text-secondary'"
            @click="inputMode = 'file'"
          >
            <i class="bi bi-file-earmark-code me-2"></i>Файли
          </button>
          <button
            class="btn btn-sm rounded-pill px-4 fw-bold"
            :class="inputMode === 'text' ? 'btn-primary' : 'btn-light text-secondary'"
            @click="inputMode = 'text'"
          >
            <i class="bi bi-code-square me-2"></i>Текст коду
          </button>
          <button
            class="btn btn-sm rounded-pill px-4 fw-bold"
            :class="inputMode === 'storage' ? 'btn-primary' : 'btn-light text-secondary'"
            @click="inputMode = 'storage'"
          >
            <i class="bi bi-cloud-check me-2"></i>Зі сховища
          </button>
        </div>

        <select v-if="inputMode === 'text'" v-model="selectedLanguage" class="form-select form-select-sm w-auto shadow-sm rounded-pill px-3 py-1 border-primary border-opacity-25 fw-bold text-primary" style="height: 34px;">
          <option value="python">Python (.py)</option>
          <option value="csharp">C# (.cs)</option>
          <option value="javascript">JavaScript (.js)</option>
          <option value="typescript">TypeScript (.ts)</option>
          <option value="java">Java (.java)</option>
          <option value="cpp">C++ (.cpp)</option>
          <option value="go">Go (.go)</option>
          <option value="php">PHP (.php)</option>
        </select>
      </div>

      <div v-if="inputMode === 'file'" class="row g-4 justify-content-center align-items-stretch">
        <div class="col-md-5">
          <div class="card h-100 border-0 shadow-lg rounded-4 overflow-hidden position-relative"
            :class="isDragging1 ? 'bg-primary bg-opacity-10 border border-2 border-primary' : 'bg-white'"
            @dragover.prevent="isDragging1 = true" @dragleave.prevent="isDragging1 = false" @drop.prevent="(e) => handleDrop(e, 1)">
            <div class="card-body d-flex flex-column align-items-center justify-content-center p-5 text-center" style="min-height: 350px;">
              <div v-if="!file1">
                <div class="mb-4 p-4 rounded-circle bg-primary bg-opacity-10 text-primary d-inline-flex shadow-sm">
                  <i class="bi bi-cloud-arrow-up-fill display-4"></i>
                </div>
                <h4 class="fw-bold mb-2 text-dark">Файл 1</h4>
                <p class="text-muted mb-4 small">Перетягніть файл сюди або натисніть кнопку</p>
                <label class="btn btn-outline-primary rounded-pill px-4 py-2 fw-bold border-2">
                  <i class="bi bi-folder-plus me-2"></i> Обрати файл
                  <input type="file" hidden @change="(e) => handleFileSelect(e, 1)" accept=".cs,.py,.js,.ts,.java,.cpp,.h,.html,.css,.php,.rb,.go" />
                </label>
              </div>
              <div v-else>
                <div class="mb-3 text-success position-relative">
                  <i class="bi bi-file-earmark-check-fill display-1"></i>
                  <span class="position-absolute top-0 start-100 translate-middle p-2 bg-success border border-light rounded-circle"></span>
                </div>
                <h5 class="fw-bold text-break mb-1 text-dark">{{ file1.name }}</h5>
                <span class="badge bg-light text-secondary border mb-4 px-3 py-2 rounded-pill">{{ (file1.size / 1024).toFixed(2) }} KB</span>
                <div>
                  <button class="btn btn-light text-danger btn-sm rounded-pill px-4 py-2 fw-bold shadow-sm" @click="removeFile(1)">
                    <i class="bi bi-trash3-fill me-1"></i> Видалити
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div class="col-md-auto d-flex align-items-center justify-content-center py-3 py-md-0">
          <div class="bg-white rounded-circle shadow-lg p-3 text-primary fw-bold fs-4 d-flex align-items-center justify-content-center border border-4 border-light" style="width: 70px; height: 70px; z-index: 10;">
            <i class="bi bi-arrow-left-right"></i>
          </div>
        </div>
        <div class="col-md-5">
          <div class="card h-100 border-0 shadow-lg rounded-4 overflow-hidden position-relative"
            :class="isDragging2 ? 'bg-primary bg-opacity-10 border border-2 border-primary' : 'bg-white'"
            @dragover.prevent="isDragging2 = true" @dragleave.prevent="isDragging2 = false" @drop.prevent="(e) => handleDrop(e, 2)">
            <div class="card-body d-flex flex-column align-items-center justify-content-center p-5 text-center" style="min-height: 350px;">
              <div v-if="!file2">
                <div class="mb-4 p-4 rounded-circle bg-primary bg-opacity-10 text-primary d-inline-flex shadow-sm">
                  <i class="bi bi-cloud-arrow-up-fill display-4"></i>
                </div>
                <h4 class="fw-bold mb-2 text-dark">Файл 2</h4>
                <p class="text-muted mb-4 small">Перетягніть файл сюди або натисніть кнопку</p>
                <label class="btn btn-outline-primary rounded-pill px-4 py-2 fw-bold border-2">
                  <i class="bi bi-folder-plus me-2"></i> Обрати файл
                  <input type="file" hidden @change="(e) => handleFileSelect(e, 2)" accept=".cs,.py,.js,.ts,.java,.cpp,.h,.html,.css,.php,.rb,.go" />
                </label>
              </div>
              <div v-else>
                <div class="mb-3 text-success position-relative">
                  <i class="bi bi-file-earmark-check-fill display-1"></i>
                  <span class="position-absolute top-0 start-100 translate-middle p-2 bg-success border border-light rounded-circle"></span>
                </div>
                <h5 class="fw-bold text-break mb-1 text-dark">{{ file2.name }}</h5>
                <span class="badge bg-light text-secondary border mb-4 px-3 py-2 rounded-pill">{{ (file2.size / 1024).toFixed(2) }} KB</span>
                <div>
                  <button class="btn btn-light text-danger btn-sm rounded-pill px-4 py-2 fw-bold shadow-sm" @click="removeFile(2)">
                    <i class="bi bi-trash3-fill me-1"></i> Видалити
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="inputMode === 'text'" class="row g-4 justify-content-center align-items-stretch">
        <div class="col-md-5">
          <div class="card h-100 border-0 shadow-lg rounded-4 overflow-hidden bg-white">
            <div class="card-body p-0 d-flex flex-column h-100">
              <div class="p-3 border-bottom bg-light d-flex justify-content-between align-items-center">
                <span class="fw-bold text-secondary small"><i class="bi bi-code-slash me-2"></i>Фрагмент 1</span>
                <button v-if="code1" @click="code1 = ''" class="btn btn-link p-0 text-muted small text-decoration-none">Очистити</button>
              </div>
              <textarea v-model="code1" class="form-control border-0 flex-grow-1 shadow-none p-3"
                style="resize: none; font-family: 'Fira Code', monospace; font-size: 0.9rem; min-height: 350px;"
                placeholder="Вставте ваш код сюди..."></textarea>
            </div>
          </div>
        </div>
        <div class="col-md-auto d-flex align-items-center justify-content-center py-3 py-md-0">
          <div class="bg-white rounded-circle shadow-lg p-3 text-primary fw-bold fs-4 d-flex align-items-center justify-content-center border border-4 border-light" style="width: 70px; height: 70px; z-index: 10;">
            <i class="bi bi-arrow-left-right"></i>
          </div>
        </div>
        <div class="col-md-5">
          <div class="card h-100 border-0 shadow-lg rounded-4 overflow-hidden bg-white">
            <div class="card-body p-0 d-flex flex-column h-100">
              <div class="p-3 border-bottom bg-light d-flex justify-content-between align-items-center">
                <span class="fw-bold text-secondary small"><i class="bi bi-code-slash me-2"></i>Фрагмент 2</span>
                <button v-if="code2" @click="code2 = ''" class="btn btn-link p-0 text-muted small text-decoration-none">Очистити</button>
              </div>
              <textarea v-model="code2" class="form-control border-0 flex-grow-1 shadow-none p-3"
                style="resize: none; font-family: 'Fira Code', monospace; font-size: 0.9rem; min-height: 350px;"
                placeholder="Вставте еталонний код сюди..."></textarea>
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="inputMode === 'storage'" class="row g-4 justify-content-center align-items-stretch">
        <div class="col-md-5 d-flex flex-column">
          <div class="d-flex justify-content-center align-items-center gap-3 mb-3" style="min-height: 38px;">
            <div class="bg-white p-1 rounded-pill shadow-sm border">
              <button class="btn btn-sm rounded-pill px-3 fw-bold" :class="storageLeftMode === 'file' ? 'btn-primary' : 'btn-light text-secondary'" @click="storageLeftMode = 'file'">Файл</button>
              <button class="btn btn-sm rounded-pill px-3 fw-bold" :class="storageLeftMode === 'text' ? 'btn-primary' : 'btn-light text-secondary'" @click="storageLeftMode = 'text'">Текст</button>
            </div>
            
            <select v-if="storageLeftMode === 'text'" v-model="selectedLanguage" class="form-select form-select-sm w-auto shadow-sm rounded-pill px-3 py-1 border-primary border-opacity-25 fw-bold text-primary" style="height: 34px;">
              <option value="python">Python (.py)</option>
              <option value="csharp">C# (.cs)</option>
              <option value="javascript">JavaScript (.js)</option>
              <option value="typescript">TypeScript (.ts)</option>
              <option value="java">Java (.java)</option>
              <option value="cpp">C++ (.cpp)</option>
              <option value="go">Go (.go)</option>
              <option value="php">PHP (.php)</option>
            </select>
          </div>
          
          <div v-if="storageLeftMode === 'file'" class="card h-100 border-0 shadow-lg rounded-4 overflow-hidden position-relative flex-grow-1"
            :class="isDragging1 ? 'bg-primary bg-opacity-10 border border-2 border-primary' : 'bg-white'"
            @dragover.prevent="isDragging1 = true" @dragleave.prevent="isDragging1 = false" @drop.prevent="(e) => handleDrop(e, 1)">
            <div class="card-body d-flex flex-column align-items-center justify-content-center p-5 text-center flex-grow-1" style="min-height: 350px;">
              <div v-if="!file1">
                <div class="mb-4 p-4 rounded-circle bg-primary bg-opacity-10 text-primary d-inline-flex shadow-sm">
                  <i class="bi bi-cloud-arrow-up-fill display-4"></i>
                </div>
                <h4 class="fw-bold mb-2 text-dark">Файл</h4>
                <p class="text-muted mb-4 small">Перетягніть файл сюди або натисніть кнопку</p>
                <label class="btn btn-outline-primary rounded-pill px-4 py-2 fw-bold border-2">
                  <i class="bi bi-folder-plus me-2"></i> Обрати файл
                  <input type="file" hidden @change="(e) => handleFileSelect(e, 1)" accept=".cs,.py,.js,.ts,.java,.cpp,.h,.html,.css,.php,.rb,.go" />
                </label>
              </div>
              <div v-else>
                <div class="mb-3 text-success position-relative">
                  <i class="bi bi-file-earmark-check-fill display-1"></i>
                  <span class="position-absolute top-0 start-100 translate-middle p-2 bg-success border border-light rounded-circle"></span>
                </div>
                <h5 class="fw-bold text-break mb-1 text-dark">{{ file1.name }}</h5>
                <span class="badge bg-light text-secondary border mb-4 px-3 py-2 rounded-pill">{{ (file1.size / 1024).toFixed(2) }} KB</span>
                <div>
                  <button class="btn btn-light text-danger btn-sm rounded-pill px-4 py-2 fw-bold shadow-sm" @click="removeFile(1)">
                    <i class="bi bi-trash3-fill me-1"></i> Видалити
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div v-else class="card h-100 border-0 shadow-lg rounded-4 overflow-hidden bg-white flex-grow-1">
            <div class="card-body p-0 d-flex flex-column h-100">
              <div class="p-3 border-bottom bg-light d-flex justify-content-between align-items-center">
                <span class="fw-bold text-secondary small"><i class="bi bi-code-slash me-2"></i>Текст</span>
                <button v-if="code1" @click="code1 = ''" class="btn btn-link p-0 text-muted small text-decoration-none">Очистити</button>
              </div>
              <textarea v-model="code1" class="form-control border-0 flex-grow-1 shadow-none p-3"
                style="resize: none; font-family: 'Fira Code', monospace; font-size: 0.9rem; min-height: 350px;"
                placeholder="Вставте ваш код сюди..."></textarea>
            </div>
          </div>
        </div>

        <div class="col-md-auto d-flex align-items-center justify-content-center py-3 py-md-0">
          <div class="bg-white rounded-circle shadow-lg p-3 text-primary fw-bold fs-4 d-flex align-items-center justify-content-center border border-4 border-light" style="width: 70px; height: 70px; z-index: 10;">
            <i class="bi bi-arrow-left-right"></i>
          </div>
        </div>

        <div class="col-md-5 d-flex flex-column">
          <div class="d-none d-md-block mb-3" style="height: 38px;"></div>
          
          <div class="card h-100 border-0 shadow-lg rounded-4 overflow-hidden bg-white flex-grow-1">
            <div class="card-body p-0 d-flex flex-column h-100">
              <div class="p-3 border-bottom bg-light d-flex justify-content-between align-items-center">
                <span class="fw-bold text-secondary small"><i class="bi bi-hdd-network me-2"></i>Моє сховище</span>
                <button @click="loadStorageTree" class="btn btn-link p-0 text-primary small text-decoration-none">
                  <i class="bi bi-arrow-clockwise"></i> Оновити
                </button>
              </div>
              <div class="p-3 overflow-auto flex-grow-1" style="min-height: 350px;">
                <div v-if="isStorageLoading" class="text-center py-5">
                  <div class="spinner-border text-primary opacity-50"></div>
                </div>
                <div v-else-if="!authState.isAuthenticated" class="text-center py-5 text-muted">
                  <i class="bi bi-lock-fill display-4 d-block mb-3 opacity-50"></i>
                  <p class="small">Увійдіть в акаунт, щоб отримати доступ до збережених файлів.</p>
                </div>
                <div v-else-if="rootNodes.length === 0" class="text-center py-5 text-muted">
                  <i class="bi bi-folder-x display-4 d-block mb-3 opacity-25"></i>
                  <p class="small">Ваше сховище порожнє.</p>
                </div>
                <div v-else>
                  <FileTreeItem 
                    v-for="node in rootNodes" 
                    :key="node.id" 
                    :node="node" 
                    :selected-ids="selectedStorageIds" 
                    @toggle-selection="toggleStorageSelection" 
                  />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="row mt-5 justify-content-center">
        <div class="col-lg-9">
          <div class="card shadow-lg border-0 rounded-4 overflow-hidden">
            <div class="row g-0">
              <div class="col-md-8 bg-white p-5">
                <h5 class="fw-bold mb-4 d-flex align-items-center">
                  <i class="bi bi-sliders2 text-primary me-2"></i> Методи аналізу
                </h5>

                <div class="row g-4">
                  <div v-for="cat in analysisConfig" :key="cat.id" class="col-12">
                    <div class="p-3 rounded-3 border bg-light bg-opacity-50">
                      <div class="form-check form-switch mb-2">
                        <input class="form-check-input" type="checkbox" :id="'cat-' + cat.id"
                          :checked="selectedMethods[cat.id]?.length === cat.methods.length"
                          @change="(e: Event) => {
                            if((e.target as HTMLInputElement).checked) selectedMethods[cat.id] = cat.methods.map(m => m.value);
                            else delete selectedMethods[cat.id];
                          }">
                        <label class="form-check-label fw-bold text-dark" :for="'cat-' + cat.id">{{ cat.name }}</label>
                      </div>

                      <div class="d-flex flex-wrap gap-2 ps-2 pt-1">
                        <div v-for="method in cat.methods" :key="method.value" class="form-check form-check-inline m-0 bg-white px-2 py-1 rounded border">
                          <input class="form-check-input ms-0 me-2" type="checkbox" :id="'m-' + cat.id + '-' + method.value"
                            :value="method.value" :checked="selectedMethods[cat.id]?.includes(method.value)"
                            @change="toggleMethod(cat.id, method.value)">
                          <label class="form-check-label small" :for="'m-' + cat.id + '-' + method.value" style="font-size: 0.85rem;">
                            {{ method.label }}
                          </label>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div class="col-md-4 bg-primary bg-gradient text-white p-5 d-flex flex-column justify-content-center align-items-center text-center position-relative overflow-hidden">
                <div class="position-relative z-1 w-100">
                  <h4 class="fw-bold mb-2">Готові до аналізу?</h4>
                  <p class="text-white-50 mb-4 small">Перевірте вибрані файли та налаштування.</p>

                  <button
                    class="btn btn-light btn-lg w-100 rounded-pill shadow fw-bold text-primary py-3"
                    @click="startComparison"
                    :disabled="isLoading || 
                      (inputMode === 'file' && (!file1 || !file2)) || 
                      (inputMode === 'text' && (!code1 || !code2)) ||
                      (inputMode === 'storage' && ((storageLeftMode === 'file' && !file1) || (storageLeftMode === 'text' && !code1) || selectedStorageIds.length === 0))"
                  >
                    <span v-if="isLoading" class="spinner-border spinner-border-sm me-2"></span>
                    <i v-else class="bi bi-lightning-charge-fill me-2"></i>
                    {{ isLoading ? 'Аналіз...' : 'Запустити' }}
                  </button>
                </div>
              </div>

            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>