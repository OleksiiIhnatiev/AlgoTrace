<script setup lang="ts">
import { ref, watch, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { authService, authState } from '../services/auth.service';
import { analysisService, analysisState, type AnalysisPayload, type AnalysisMultiplePayload, type FileContent } from '../services/analysis.service';
import { isDarkMode, toggleTheme } from '../composables/useTheme';
import api from '../services/api';
import FileTreeItem, { type Node } from '@/components/FileTreeItem.vue';

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

const rootNodes = ref<Node[]>([]);
const selectedStorageIds = ref<string[]>([]);
const isStorageLoading = ref(false);

const selectedLanguage = ref('python');

const ignoreComments = ref(true);
const ignoreWhitespace = ref(true);

const languageOptions = [
  { group: 'C# / .NET', options: [ { value: 'csharp', label: 'C# (.cs)' } ] },
  { group: 'Web (Frontend / Backend)', options: [ { value: 'typescript', label: 'TypeScript (.ts, .tsx)' }, { value: 'javascript', label: 'JavaScript (.js, .jsx)' } ] },
  { group: 'Java / Kotlin', options: [ { value: 'java', label: 'Java (.java)' }, { value: 'kotlin', label: 'Kotlin (.kt, .kts)' } ] },
  { group: 'Python', options: [ { value: 'python', label: 'Python (.py)' } ] },
  { group: 'C / C++', options: [ { value: 'c', label: 'C (.c, .h)' }, { value: 'cpp', label: 'C++ (.cpp, .hpp)' } ] },
  { group: 'PHP / Ruby', options: [ { value: 'php', label: 'PHP (.php)' }, { value: 'ruby', label: 'Ruby (.rb)' } ] },
  { group: 'Go / Rust', options: [ { value: 'go', label: 'Go (.go)' }, { value: 'rust', label: 'Rust (.rs)' } ] },
  { group: 'Swift / Objective-C', options: [ { value: 'swift', label: 'Swift (.swift)' }, { value: 'objectivec', label: 'Objective-C (.m)' } ] },
  { group: 'Databases', options: [ { value: 'sql', label: 'SQL (.sql)' } ] },
  { group: 'Scripts', options: [ { value: 'shell', label: 'Shell (.sh, .bash)' }, { value: 'bat', label: 'Batch (.bat)' }, { value: 'powershell', label: 'PowerShell (.ps1)' } ] }
];

const selectedLanguageLabel = computed(() => {
  for (const g of languageOptions) {
    const opt = g.options.find(o => o.value === selectedLanguage.value);
    if (opt) return opt.label;
  }
  return 'Python (.py)';
});

const isLangDropdownOpen = ref(false);
const langDropdownDirection = ref<'down' | 'up'>('down');

const toggleLangDropdown = (e: MouseEvent) => {
  if (isLangDropdownOpen.value) {
    isLangDropdownOpen.value = false;
    return;
  }
  const target = e.currentTarget as HTMLElement;
  const rect = target.getBoundingClientRect();
  const spaceBelow = window.innerHeight - rect.bottom;

  langDropdownDirection.value = spaceBelow < 380 ? 'up' : 'down';
  isLangDropdownOpen.value = true;
};

const closeDropdown = (e: Event) => {
  const target = e.target as HTMLElement;
  if (!target.closest('.custom-lang-select')) isLangDropdownOpen.value = false;
};

onMounted(() => document.addEventListener('click', closeDropdown));
onUnmounted(() => document.removeEventListener('click', closeDropdown));

const analysisConfig = [
  {
    id: 'text',
    name: 'Текстовий Аналіз',
    icon: 'bi-file-text',
    methods: [
      { value: 'levenshtein', label: 'Відстань Левенштейна' },
      { value: 'line_matching', label: 'Порядкове Порівняння' },
      { value: 'rabin_karp', label: 'Алгоритм Рабіна-Карпа' },
      { value: 'ngram_search', label: 'Пошук за N-грамами' }
    ]
  },
  {
    id: 'token',
    name: 'Токенний Аналіз',
    icon: 'bi-braces',
    methods: [
      { value: 'jaccard_token', label: 'Токени Джаккарда' },
      { value: 'winnowing', label: 'Вінновінг (Winnowing)' }
    ]
  },
  {
    id: 'tree',
    name: 'Аналіз AST-дерев',
    icon: 'bi-diagram-3',
    methods: [
      { value: 'ast_hashing', label: 'Хешування AST' },
      { value: 'ast_compare', label: 'Пряме Порівняння AST' },
      { value: 'subtree_isomorphism', label: 'Ізоморфізм Піддерев' }
    ]
  },
  {
    id: 'graph',
    name: 'Аналіз Графів (CFG/PDG)',
    icon: 'bi-share',
    methods: [
      { value: 'cfg', label: 'Граф Потоку Керування (CFG)' },
      { value: 'pdg', label: 'Граф Залежностей Даних (PDG)' },
      { value: 'subgraph_isomorphism', label: 'Ізоморфізм Підграфів' }
    ]
  },
  {
    id: 'metric',
    name: 'Метрики Коду',
    icon: 'bi-bar-chart-steps',
    methods: [
      { value: 'halstead', label: 'Метрики Холстеда' },
      { value: 'mccabe', label: 'Складність Маккейба' }
    ]
  }
];

const selectedMethods = ref<Record<string, string[]>>({});

const isAllMethodsSelected = computed(() => {
  return analysisConfig.every(cat => selectedMethods.value[cat.id]?.length === cat.methods.length);
});

const toggleAllMethods = (e: Event) => {
  const isChecked = (e.target as HTMLInputElement).checked;
  if (isChecked) {
    analysisConfig.forEach(cat => {
      selectedMethods.value[cat.id] = cat.methods.map(m => m.value);
    });
  } else {
    selectedMethods.value = {};
  }
};

const detectLanguage = (code: string): string | null => {
  if (!code) return null;
  const c = code.toLowerCase();
  if (c.includes('using system;') || (c.includes('namespace ') && c.includes('class '))) return 'csharp';
  if (c.includes('import java.') || (c.includes('public class ') && c.includes('public static void main'))) return 'java';
  if (c.includes('def ') && c.includes(':') && !c.includes('{')) return 'python';
  if (c.includes('import {') || c.includes('export const ') || c.includes('interface ') || c.includes('type ')) return 'typescript';
  if (c.includes('#include <') || c.includes('int main()')) return 'cpp';
  if (c.includes('<?php')) return 'php';
  if (c.includes('package main') && c.includes('import "fmt"')) return 'go';
  if (c.includes('fn main()') && c.includes('println!')) return 'rust';
  if (c.includes('<html>') || c.includes('</div>')) return 'html';
  if (c.includes('<?xml')) return 'xml';
  if (c.includes('select ') && c.includes(' from ')) return 'sql';
  if (c.includes('import react') || c.includes('from \'react\'') || c.includes('classname=')) return 'javascript';
  if (c.includes('function ') || c.includes('const ') || c.includes('let ') || c.includes('console.log')) return 'javascript';
  if (c.trim().startsWith('{') && c.trim().endsWith('}')) {
    try { JSON.parse(code); return 'json'; } catch {}
  }
  return null;
};

watch(code1, (newVal, oldVal) => {
  if (newVal && Math.abs(newVal.length - (oldVal?.length || 0)) > 20) {
    const detected = detectLanguage(newVal);
    if (detected) {
      selectedLanguage.value = detected;
    }
  }
});

const loadStorageTree = async () => {
  if (!authState.isAuthenticated) return;
  isStorageLoading.value = true;
  try {
    const res = await api.get('/api/directory/folder');
    const folders = res.data.folders.map((f: { folderId: string; name: string }) => ({ id: f.folderId, name: f.name, type: 'folder' }));
    const files = res.data.files.map((f: { fileId: string; name: string }) => ({ id: f.fileId, name: f.name, type: 'file' }));
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

watch(() => authState.isAuthenticated, (isAuth) => {
  if (!isAuth && inputMode.value === 'storage') {
    inputMode.value = 'file';
  }
});

const checkNodeAndChildren = async (id: string) => {
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
  }
};

const uncheckNodeAndChildren = async (id: string) => {
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
  }
};

const toggleStorageSelection = async (node: Node) => {
  const isSelected = selectedStorageIds.value.includes(node.id);

  if (node.type === 'file') {
    if (isSelected) {
      selectedStorageIds.value = selectedStorageIds.value.filter(selId => selId !== node.id);
    } else {
      selectedStorageIds.value.push(node.id);
    }
    return;
  }

  if (isSelected) {
    await uncheckNodeAndChildren(node.id);
  } else {
    await checkNodeAndChildren(node.id);
  }
};

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
  const allowedExtensions = [
    '.cs', '.ts', '.tsx', '.js', '.jsx', '.html', '.css', '.scss', '.sass', '.less',
    '.java', '.kt', '.kts', '.py', '.c', '.cpp', '.h', '.hpp', '.php', '.rb',
    '.go', '.rs', '.swift', '.m', '.sql', '.json', '.xml', '.yaml', '.yml',
    '.sh', '.bash', '.bat', '.ps1'
  ];
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
      if (storageLeftMode.value === 'file') {
        const res1 = await analysisService.uploadFile(file1.value!);
        language = res1.data.language || 'python';
        submissionAData = res1.data.submission;
      } else {
        language = selectedLanguage.value;
        submissionAData = { files: [{ filename: "submission.txt", content: code1.value }] };
      }
    }

    const categoryMap: Record<string, string> = {
      'text': 'text_based',
      'token': 'token_based',
      'tree': 'tree_based',
      'graph': 'graph_based',
      'metric': 'metrics_based'
    };

      if (inputMode.value === 'storage') {
        const executeCategories: { category_name: string; methods: string[] }[] = [];

        for (const [catId, methods] of Object.entries(selectedMethods.value)) {
          if (methods && methods.length > 0) {
            const apiCategoryKey = categoryMap[catId] || catId;
            executeCategories.push({
              category_name: apiCategoryKey,
              methods: methods
            });
          }
        }

        const payload: AnalysisMultiplePayload = {
          language: language,
          submission: submissionAData!,
          compare_with_document_ids: selectedStorageIds.value,
          analysis_config: {
            categories: executeCategories,
            parameters: { ignore_comments: ignoreComments.value, ignore_whitespace: ignoreWhitespace.value }
          }
        };

        const response = await analysisService.analyzeMultiple(payload);

        if (response.data) {
          analysisState.currentReport = response.data as Record<string, unknown>;
          router.push('/analyzer');
        }
      } else {
        const executeCategories: Record<string, string[]> = {};

        for (const [catId, methods] of Object.entries(selectedMethods.value)) {
          if (methods && methods.length > 0) {
            const apiCategoryKey = categoryMap[catId];
            if (apiCategoryKey) {
              executeCategories[apiCategoryKey] = methods;
            }
          }
        }

        const payload: AnalysisPayload = {
          language: language,
          submission_a: submissionAData!,
          submission_b: submissionBData!,
          analysis_config: {
            parameters: { ignore_comments: ignoreComments.value, ignore_whitespace: ignoreWhitespace.value },
            execute_categories: executeCategories
          }
        };

        const response = await analysisService.analyze('/api/analysis/unified', payload);

        if (response.data) {
          analysisState.currentReport = response.data as Record<string, unknown>;
          router.push('/analyzer');
        }
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
  <div class="min-vh-100 dashboard-bg pb-5 position-relative">
    <nav class="navbar navbar-expand-lg navbar-light bg-white bg-opacity-75 border-bottom py-3 backdrop-blur sticky-top z-3 shadow-sm">
      <div class="container-fluid px-4">
        <a class="navbar-brand fw-black d-flex align-items-center text-dark" href="#" style="letter-spacing: -0.5px;">
          <i class="bi bi-layers-half text-primary fs-3 me-2 glow-text-primary"></i>
          AlgoTrace <span class="text-primary ms-1">Nexus</span>
        </a>

        <div class="d-flex align-items-center">
          <button @click="toggleTheme" class="btn btn-light rounded-circle shadow-sm border hover-lift me-3 d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;" title="Змінити тему">
            <i class="bi fs-5" :class="isDarkMode ? 'bi-sun-fill text-warning' : 'bi-moon-stars-fill text-secondary'"></i>
          </button>
          <div v-if="!authState.isAuthenticated" class="dropdown">
            <router-link to="/auth" class="btn btn-primary rounded-pill px-4 py-2 fw-bold shadow-sm hover-glow d-flex align-items-center">
              <i class="bi bi-person-circle me-2 fs-5"></i> Увійти
            </router-link>
          </div>

          <div v-else class="dropdown">
            <a href="#" class="d-flex align-items-center text-dark text-decoration-none px-4 py-2 rounded-pill bg-light bg-opacity-75 border shadow-sm dropdown-toggle hover-lift" data-bs-toggle="dropdown">
              <i class="bi bi-person-check-fill fs-5 me-2 text-primary"></i>
              <span class="fw-bold small">{{ authState.user?.email }}</span>
            </a>
            <ul class="dropdown-menu dropdown-menu-end border-0 shadow-lg rounded-4 mt-2 p-2" style="min-width: 200px;">
              <li>
                <router-link to="/storage" class="dropdown-item fw-bold py-2 rounded-3 text-dark mb-1 hover-lift">
                  <i class="bi bi-folder2-open me-2 text-primary"></i> Мої файли
                </router-link>
              </li>
              <li><hr class="dropdown-divider opacity-25"></li>
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

    <div v-if="authState.isAuthenticated && !authState.user?.isEmailConfirmed" class="alert bg-warning bg-opacity-25 border-0 border-bottom border-warning border-opacity-50 text-center rounded-0 mb-0 shadow-sm backdrop-blur py-2 text-dark fw-medium" style="font-size: 0.9rem;">
      <i class="bi bi-exclamation-triangle-fill text-warning me-2 fs-5 align-middle"></i>
      Увага: Ваш email не підтверджено. Деякі функції можуть бути обмежені.
      <a href="#" @click.prevent="resendEmail" class="text-primary fw-bold ms-2 text-decoration-none hover-glow">Надіслати лист повторно <i class="bi bi-arrow-right-short"></i></a>
    </div>

    <div class="container-fluid px-4 px-xxl-5 pb-5 position-relative z-1 mt-5">
      <div class="text-center mb-5 position-relative z-1 animate__fadeIn">
        <div class="d-inline-flex align-items-center justify-content-center p-4 rounded-circle bg-primary bg-opacity-10 mb-4 shadow-sm" style="width: 90px; height: 90px;">
           <i class="bi bi-infinity display-4 text-primary glow-text-primary"></i>
        </div>
        <h1 class="display-3 fw-black text-dark mb-3 tracking-tight" style="letter-spacing: -1.5px;">
          Глибока Аналітика <span class="text-primary glow-text-primary">Коду</span>
        </h1>
        <p class="lead text-secondary mx-auto col-lg-6 fw-medium">
          Синтезуйте, аналізуйте та розкривайте структурні зв'язки у вашому коді за допомогою надсучасних багатовекторних алгоритмів.
        </p>
      </div>

      <div class="glass-card p-4 p-md-5 mb-5 position-relative overflow-hidden shadow-lg border-0 animate__fadeIn" style="animation-delay: 0.1s;">

        <div class="d-flex justify-content-center mb-5 position-relative z-1">
          <div class="bg-white bg-opacity-75 p-1 rounded-pill shadow-sm border d-inline-flex align-items-center">
            <button class="btn btn-glass rounded-pill px-4 px-md-5 py-2 fw-bold text-uppercase tracking-wider" :class="{ 'active': inputMode === 'file' }" @click="inputMode = 'file'">
              <i class="bi bi-file-earmark-code me-2"></i> Файли
            </button>
            <button class="btn btn-glass rounded-pill px-4 px-md-5 py-2 fw-bold text-uppercase tracking-wider" :class="{ 'active': inputMode === 'text' }" @click="inputMode = 'text'">
              <i class="bi bi-code-square me-2"></i> Текст
            </button>
            <button class="btn btn-glass rounded-pill px-4 px-md-5 py-2 fw-bold text-uppercase tracking-wider transition-all"
                    :class="{ 'active': inputMode === 'storage' }"
                    @click="authState.isAuthenticated ? inputMode = 'storage' : null"
                    :style="!authState.isAuthenticated ? 'cursor: not-allowed; opacity: 0.7;' : ''"
                    :title="!authState.isAuthenticated ? 'Необхідна авторизація для доступу' : ''">
              <i class="bi" :class="authState.isAuthenticated ? 'bi-cloud-check me-2' : 'bi-lock-fill me-2 text-warning'"></i> Сховище
            </button>
          </div>
        </div>

        <div v-if="inputMode === 'file'" class="row g-4 justify-content-center align-items-center position-relative z-1">
          <div class="col-lg">
            <div class="glass-panel p-4 text-center d-flex flex-column align-items-center justify-content-center border-dashed rounded-4 position-relative transition-all"
                 style="min-height: 360px; border: 2px dashed rgba(13,110,253,0.3);"
                 :class="{'dropzone-active': isDragging1}"
                 @dragover.prevent="isDragging1 = true" @dragleave.prevent="isDragging1 = false" @drop.prevent="(e) => handleDrop(e, 1)">
              <div v-if="!file1" class="text-center w-100 animate__fadeIn">
                <div class="mb-4 p-4 rounded-circle bg-white shadow-sm d-inline-flex position-relative icon-container">
                  <i class="bi bi-cloud-arrow-up text-primary display-4 glow-text-primary"></i>
                </div>
                <h4 class="fw-black mb-2 text-dark tracking-tight">Перший Файл</h4>
                <p class="text-secondary mb-4 small fw-medium">Перетягніть сюди або виберіть вручну</p>
                <label class="btn btn-primary rounded-pill px-5 py-3 fw-bold shadow-sm d-inline-flex align-items-center hover-lift cursor-pointer">
                  <i class="bi bi-search me-2 fs-5"></i> Огляд файлів
                  <input type="file" hidden @change="(e) => handleFileSelect(e, 1)" accept=".cs,.ts,.tsx,.js,.jsx,.html,.css,.scss,.sass,.less,.java,.kt,.kts,.py,.c,.cpp,.h,.hpp,.php,.rb,.go,.rs,.swift,.m,.sql,.json,.xml,.yaml,.yml,.sh,.bash,.bat,.ps1" />
                </label>
              </div>
              <div v-else class="text-center w-100 animate__fadeIn">
                <div class="mb-4 p-4 rounded-circle bg-success bg-opacity-10 d-inline-flex position-relative">
                  <i class="bi bi-file-earmark-check-fill text-success display-4" style="text-shadow: 0 0 20px rgba(16, 185, 129, 0.4);"></i>
                </div>
                <h5 class="fw-black text-break mb-2 text-dark font-monospace">{{ file1.name }}</h5>
                <div class="d-flex align-items-center justify-content-center gap-3 mb-4">
                  <span class="badge bg-white text-secondary border px-3 py-2 rounded-pill shadow-sm fw-medium">{{ (file1.size / 1024).toFixed(2) }} KB</span>
                  <span class="badge bg-primary bg-opacity-10 text-primary border border-primary border-opacity-25 px-3 py-2 rounded-pill shadow-sm fw-bold">Готово</span>
                </div>
                <button class="btn btn-outline-danger rounded-pill px-4 py-2 fw-bold d-inline-flex align-items-center hover-lift" @click="removeFile(1)">
                  <i class="bi bi-trash3-fill me-2"></i> Видалити
                </button>
              </div>
            </div>
          </div>
          <div class="col-lg-auto px-xl-4 d-flex justify-content-center py-3 py-lg-0">
             <div class="d-flex align-items-center justify-content-center rounded-circle bg-white shadow-lg border border-4 border-white position-relative z-2" style="width: 80px; height: 80px;">
                <i class="bi bi-bezier2 text-primary fs-2 glow-text-primary d-none d-lg-block"></i>
                <i class="bi bi-arrow-down text-primary fs-2 glow-text-primary d-block d-lg-none"></i>
             </div>
          </div>
          <div class="col-lg">
            <div class="glass-panel p-4 text-center d-flex flex-column align-items-center justify-content-center border-dashed rounded-4 position-relative transition-all"
                 style="min-height: 360px; border: 2px dashed rgba(220, 53, 69, 0.3);"
                 :class="{'dropzone-active': isDragging2}"
                 @dragover.prevent="isDragging2 = true" @dragleave.prevent="isDragging2 = false" @drop.prevent="(e) => handleDrop(e, 2)">
              <div v-if="!file2" class="text-center w-100 animate__fadeIn">
                <div class="mb-4 p-4 rounded-circle bg-white shadow-sm d-inline-flex position-relative icon-container">
                  <i class="bi bi-cloud-arrow-up text-danger display-4" style="text-shadow: 0 0 20px rgba(220, 53, 69, 0.5);"></i>
                </div>
                <h4 class="fw-black mb-2 text-dark tracking-tight">Другий Файл</h4>
                <p class="text-secondary mb-4 small fw-medium">Перетягніть сюди або виберіть вручну</p>
                <label class="btn btn-danger rounded-pill px-5 py-3 fw-bold shadow-sm d-inline-flex align-items-center hover-lift cursor-pointer">
                  <i class="bi bi-search me-2 fs-5"></i> Огляд файлів
                  <input type="file" hidden @change="(e) => handleFileSelect(e, 2)" accept=".cs,.ts,.tsx,.js,.jsx,.html,.css,.scss,.sass,.less,.java,.kt,.kts,.py,.c,.cpp,.h,.hpp,.php,.rb,.go,.rs,.swift,.m,.sql,.json,.xml,.yaml,.yml,.sh,.bash,.bat,.ps1" />
                </label>
              </div>
              <div v-else class="text-center w-100 animate__fadeIn">
                <div class="mb-4 p-4 rounded-circle bg-success bg-opacity-10 d-inline-flex position-relative">
                  <i class="bi bi-file-earmark-check-fill text-success display-4" style="text-shadow: 0 0 20px rgba(16, 185, 129, 0.4);"></i>
                </div>
                <h5 class="fw-black text-break mb-2 text-dark font-monospace">{{ file2.name }}</h5>
                <div class="d-flex align-items-center justify-content-center gap-3 mb-4">
                  <span class="badge bg-white text-secondary border px-3 py-2 rounded-pill shadow-sm fw-medium">{{ (file2.size / 1024).toFixed(2) }} KB</span>
                  <span class="badge bg-success bg-opacity-10 text-success border border-success border-opacity-25 px-3 py-2 rounded-pill shadow-sm fw-bold">Готово</span>
                </div>
                <button class="btn btn-outline-danger rounded-pill px-4 py-2 fw-bold d-inline-flex align-items-center hover-lift" @click="removeFile(2)">
                  <i class="bi bi-trash3-fill me-2"></i> Видалити
                </button>
              </div>
            </div>
          </div>
        </div>

        <div v-else-if="inputMode === 'text'" class="row g-4 justify-content-center align-items-stretch position-relative z-1">
          <div class="col-lg">
            <div class="glass-panel h-100 rounded-4 d-flex flex-column shadow-sm" style="border: 1px solid rgba(255,255,255,0.8);">
              <div class="bg-white bg-opacity-75 px-4 py-3 border-bottom d-flex justify-content-between align-items-center backdrop-blur rounded-top-4">
                <span class="fw-black text-dark small text-uppercase tracking-wider d-flex align-items-center">
                  <i class="bi bi-terminal text-primary fs-5 me-2 glow-text-primary"></i> Фрагмент Коду 1
                </span>
                <div class="d-flex gap-2">
                   <div class="custom-lang-select position-relative" style="width: 240px;">
                      <div class="bg-light shadow-sm fw-bold text-primary rounded-pill px-4 cursor-pointer d-flex justify-content-between align-items-center text-truncate border border-primary border-opacity-10 hover-glow"
                           @click="toggleLangDropdown" style="min-height: 36px;">
                         <span class="text-truncate" style="font-size: 0.85rem;">{{ selectedLanguageLabel }}</span>
                         <i class="bi bi-chevron-down ms-2 transition-all" :class="{'rotate-180': isLangDropdownOpen}" style="font-size: 0.8rem;"></i>
                      </div>
                      <transition :name="langDropdownDirection === 'down' ? 'fade-down' : 'fade-up'">
                         <div v-if="isLangDropdownOpen" class="position-absolute end-0 bg-white rounded-4 shadow-lg border custom-scrollbar z-3 text-start"
                              :class="langDropdownDirection === 'down' ? 'top-100 mt-2' : 'bottom-100 mb-2'"
                              style="width: 290px; max-height: 360px; overflow-y: auto; overflow-x: hidden;">
                            <div v-for="group in languageOptions" :key="group.group">
                               <div class="bg-light bg-opacity-50 px-3 py-2 small fw-black text-muted text-uppercase tracking-wider border-bottom border-top" style="font-size: 0.7rem; letter-spacing: 0.05em;">{{ group.group }}</div>
                               <div v-for="opt in group.options" :key="opt.value"
                                    class="px-3 py-2 small fw-medium cursor-pointer lang-option transition-all border-bottom border-light"
                                    :class="{'bg-primary bg-opacity-10 text-primary fw-bold': selectedLanguage === opt.value, 'text-dark': selectedLanguage !== opt.value}"
                                    @click="selectedLanguage = opt.value; isLangDropdownOpen = false">
                                  {{ opt.label }} <i v-if="selectedLanguage === opt.value" class="bi bi-check-circle-fill float-end text-primary mt-1"></i>
                               </div>
                            </div>
                         </div>
                      </transition>
                   </div>
                   <button v-if="code1" @click="code1 = ''" class="btn btn-outline-danger rounded-circle shadow-none hover-lift d-flex align-items-center justify-content-center" style="width: 36px; height: 36px;"><i class="bi bi-trash3-fill"></i></button>
                </div>
              </div>
              <textarea v-model="code1" class="form-control border-0 bg-transparent flex-grow-1 p-4 shadow-none custom-scrollbar font-monospace text-dark rounded-bottom-4"
                style="resize: none; font-size: 0.85rem; min-height: 350px; line-height: 1.6; white-space: pre; overflow-wrap: normal; overflow-x: auto;"
                placeholder="// Вставте ваш код сюди..."></textarea>
            </div>
          </div>
          <div class="col-lg-auto px-xl-4 d-flex justify-content-center align-items-center py-3 py-lg-0">
             <div class="d-flex align-items-center justify-content-center rounded-circle bg-white shadow-lg border border-4 border-white position-relative z-2" style="width: 80px; height: 80px;">
                <i class="bi bi-bezier2 text-primary fs-2 glow-text-primary d-none d-lg-block"></i>
                <i class="bi bi-arrow-down text-primary fs-2 glow-text-primary d-block d-lg-none"></i>
             </div>
          </div>
          <div class="col-lg">
            <div class="glass-panel h-100 rounded-4 d-flex flex-column shadow-sm">
              <div class="bg-white bg-opacity-75 px-4 py-3 border-bottom d-flex justify-content-between align-items-center backdrop-blur rounded-top-4">
                <span class="fw-black text-dark small text-uppercase tracking-wider d-flex align-items-center">
                  <i class="bi bi-terminal text-danger fs-5 me-2" style="text-shadow: 0 0 15px rgba(220,53,69,0.5);"></i> Фрагмент Коду 2
                </span>
                <div class="d-flex gap-2">
                   <button v-if="code2" @click="code2 = ''" class="btn btn-outline-danger rounded-circle shadow-none hover-lift d-flex align-items-center justify-content-center" style="width: 36px; height: 36px;"><i class="bi bi-trash3-fill"></i></button>
                </div>
              </div>
              <textarea v-model="code2" class="form-control border-0 bg-transparent flex-grow-1 p-4 shadow-none custom-scrollbar font-monospace text-dark rounded-bottom-4"
                style="resize: none; font-size: 0.85rem; min-height: 350px; line-height: 1.6; white-space: pre; overflow-wrap: normal; overflow-x: auto;"
                placeholder="// Вставте еталонний код сюди..."></textarea>
            </div>
          </div>
        </div>

        <div v-else-if="inputMode === 'storage'" class="row g-4 justify-content-center align-items-stretch position-relative z-1">
          <div class="col-lg d-flex flex-column">
            <div class="d-flex justify-content-between align-items-center mb-3 px-2">
               <span class="fw-black text-uppercase tracking-wider small text-muted"><i class="bi bi-file-earmark-diff text-primary me-2 fs-5"></i>Досліджуваний код</span>
               <div class="bg-white bg-opacity-75 p-1 rounded-pill shadow-sm border border-white d-inline-flex">
                 <button class="btn btn-sm btn-glass rounded-pill px-4 fw-bold" :class="{'active': storageLeftMode === 'file'}" @click="storageLeftMode = 'file'">Файл</button>
                 <button class="btn btn-sm btn-glass rounded-pill px-4 fw-bold" :class="{'active': storageLeftMode === 'text'}" @click="storageLeftMode = 'text'">Текст</button>
               </div>
            </div>

            <div v-if="storageLeftMode === 'file'" class="glass-panel p-4 text-center d-flex flex-column align-items-center justify-content-center border-dashed rounded-4 position-relative transition-all flex-grow-1"
                 style="min-height: 360px; border: 2px dashed rgba(13,110,253,0.3);"
                 :class="{'dropzone-active': isDragging1}"
                 @dragover.prevent="isDragging1 = true" @dragleave.prevent="isDragging1 = false" @drop.prevent="(e) => handleDrop(e, 1)">
              <div v-if="!file1" class="text-center w-100 animate__fadeIn">
                <div class="mb-4 p-4 rounded-circle bg-white shadow-sm d-inline-flex position-relative icon-container">
                  <i class="bi bi-cloud-arrow-up text-primary display-4 glow-text-primary"></i>
                </div>
                <h4 class="fw-black mb-2 text-dark tracking-tight">Перший Файл</h4>
                <p class="text-secondary mb-4 small fw-medium">Перетягніть сюди або виберіть вручну</p>
                <label class="btn btn-primary rounded-pill px-5 py-3 fw-bold shadow-sm d-inline-flex align-items-center hover-lift cursor-pointer">
                  <i class="bi bi-search me-2 fs-5"></i> Огляд файлів
                  <input type="file" hidden @change="(e) => handleFileSelect(e, 1)" accept=".cs,.ts,.tsx,.js,.jsx,.html,.css,.scss,.sass,.less,.java,.kt,.kts,.py,.c,.cpp,.h,.hpp,.php,.rb,.go,.rs,.swift,.m,.sql,.json,.xml,.yaml,.yml,.sh,.bash,.bat,.ps1" />
                </label>
              </div>
              <div v-else class="text-center w-100 animate__fadeIn">
                <div class="mb-4 p-4 rounded-circle bg-success bg-opacity-10 d-inline-flex position-relative">
                  <i class="bi bi-file-earmark-check-fill text-success display-4" style="text-shadow: 0 0 20px rgba(16, 185, 129, 0.4);"></i>
                </div>
                <h5 class="fw-black text-break mb-2 text-dark font-monospace">{{ file1.name }}</h5>
                <div class="d-flex align-items-center justify-content-center gap-3 mb-4">
                  <span class="badge bg-white text-secondary border px-3 py-2 rounded-pill shadow-sm fw-medium">{{ (file1.size / 1024).toFixed(2) }} KB</span>
                  <span class="badge bg-primary bg-opacity-10 text-primary border border-primary border-opacity-25 px-3 py-2 rounded-pill shadow-sm fw-bold">Готово</span>
                </div>
                <button class="btn btn-outline-danger rounded-pill px-4 py-2 fw-bold d-inline-flex align-items-center hover-lift" @click="removeFile(1)">
                  <i class="bi bi-trash3-fill me-2"></i> Видалити
                </button>
              </div>
            </div>

            <div v-else class="glass-panel h-100 rounded-4 d-flex flex-column shadow-sm flex-grow-1">
              <div class="bg-white bg-opacity-75 px-4 py-3 border-bottom d-flex justify-content-between align-items-center backdrop-blur rounded-top-4">
                <span class="fw-black text-dark small text-uppercase tracking-wider d-flex align-items-center">
                  <i class="bi bi-terminal text-primary fs-5 me-2 glow-text-primary"></i> Текст Коду
                </span>
                <div class="d-flex gap-2">
                   <div class="custom-lang-select position-relative" style="width: 240px;">
                      <div class="bg-light shadow-sm fw-bold text-primary rounded-pill px-4 cursor-pointer d-flex justify-content-between align-items-center text-truncate border border-primary border-opacity-10 hover-glow"
                           @click="toggleLangDropdown" style="min-height: 36px;">
                         <span class="text-truncate" style="font-size: 0.85rem;">{{ selectedLanguageLabel }}</span>
                         <i class="bi bi-chevron-down ms-2 transition-all" :class="{'rotate-180': isLangDropdownOpen}" style="font-size: 0.8rem;"></i>
                      </div>
                      <transition :name="langDropdownDirection === 'down' ? 'fade-down' : 'fade-up'">
                         <div v-if="isLangDropdownOpen" class="position-absolute end-0 bg-white rounded-4 shadow-lg border custom-scrollbar z-3 text-start"
                              :class="langDropdownDirection === 'down' ? 'top-100 mt-2' : 'bottom-100 mb-2'"
                              style="width: 290px; max-height: 360px; overflow-y: auto; overflow-x: hidden;">
                            <div v-for="group in languageOptions" :key="group.group">
                               <div class="bg-light bg-opacity-50 px-3 py-2 small fw-black text-muted text-uppercase tracking-wider border-bottom border-top" style="font-size: 0.7rem; letter-spacing: 0.05em;">{{ group.group }}</div>
                               <div v-for="opt in group.options" :key="opt.value"
                                    class="px-3 py-2 small fw-medium cursor-pointer lang-option transition-all border-bottom border-light"
                                    :class="{'bg-primary bg-opacity-10 text-primary fw-bold': selectedLanguage === opt.value, 'text-dark': selectedLanguage !== opt.value}"
                                    @click="selectedLanguage = opt.value; isLangDropdownOpen = false">
                                  {{ opt.label }} <i v-if="selectedLanguage === opt.value" class="bi bi-check-circle-fill float-end text-primary mt-1"></i>
                               </div>
                            </div>
                         </div>
                      </transition>
                   </div>
                   <button v-if="code1" @click="code1 = ''" class="btn btn-outline-danger rounded-circle shadow-none hover-lift d-flex align-items-center justify-content-center" style="width: 36px; height: 36px;"><i class="bi bi-trash3-fill"></i></button>
                </div>
              </div>
              <textarea v-model="code1" class="form-control border-0 bg-transparent flex-grow-1 p-4 shadow-none custom-scrollbar font-monospace text-dark rounded-bottom-4"
                style="resize: none; font-size: 0.85rem; min-height: 300px; line-height: 1.6; white-space: pre; overflow-wrap: normal; overflow-x: auto;"
                placeholder="// Вставте ваш код сюди..."></textarea>
            </div>
          </div>

          <div class="col-lg-auto px-xl-4 d-flex justify-content-center align-items-center py-3 py-lg-0">
             <div class="d-flex align-items-center justify-content-center rounded-circle bg-white shadow-lg border border-4 border-white position-relative z-2" style="width: 80px; height: 80px;">
                <i class="bi bi-diagram-3 text-primary fs-2 glow-text-primary d-none d-lg-block"></i>
                <i class="bi bi-arrow-down text-primary fs-2 glow-text-primary d-block d-lg-none"></i>
             </div>
          </div>

          <div class="col-lg d-flex flex-column">
            <div class="d-flex justify-content-between align-items-center mb-3 px-2">
              <span class="fw-black text-uppercase tracking-wider small text-muted"><i class="bi bi-database text-primary me-2 fs-5"></i>База порівняння</span>
              <button @click="loadStorageTree" class="btn btn-sm btn-light rounded-pill px-3 fw-bold text-primary shadow-sm hover-lift d-flex align-items-center">
                <i class="bi bi-arrow-clockwise me-2"></i>Оновити
              </button>
            </div>
            <div class="glass-panel h-100 rounded-4 overflow-hidden d-flex flex-column bg-primary bg-opacity-10" style="border: 1px solid rgba(13, 110, 253, 0.15); box-shadow: inset 0 0 20px rgba(13, 110, 253, 0.05);">
              <div class="p-4 overflow-auto flex-grow-1 custom-scrollbar" style="min-height: 360px;">
                 <div v-if="isStorageLoading" class="h-100 d-flex flex-column align-items-center justify-content-center text-primary">
                   <div class="spinner-border mb-3 border-2" style="width: 3rem; height: 3rem;"></div>
                   <span class="fw-bold tracking-wider text-uppercase small">Синхронізація...</span>
                 </div>
                 <div v-else-if="!authState.isAuthenticated" class="h-100 d-flex flex-column align-items-center justify-content-center text-muted">
                   <div class="p-4 rounded-circle bg-light mb-3 shadow-sm"><i class="bi bi-lock-fill display-5 opacity-50 text-dark"></i></div>
                   <p class="fw-medium text-center px-4">Авторизуйтесь, щоб отримати доступ до вашого хмарного сховища.</p>
                 </div>
                 <div v-else-if="rootNodes.length === 0" class="h-100 d-flex flex-column align-items-center justify-content-center text-muted">
                   <div class="p-4 rounded-circle bg-light mb-3 shadow-sm"><i class="bi bi-folder-x display-5 opacity-50 text-dark"></i></div>
                   <p class="fw-medium text-center">Сховище порожнє.</p>
                 </div>
                 <div v-else class="pe-2">
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

      <div class="glass-card p-4 p-md-5 mb-4 position-relative z-1 animate__fadeIn" style="animation-delay: 0.2s;">
         <div class="position-absolute top-0 end-0 p-4 opacity-10" style="pointer-events: none;">
                <i class="bi bi-gear-wide-connected" style="font-size: 8rem;"></i>
             </div>
             <div class="mb-4 position-relative z-1">
               <h4 class="fw-black mb-1 d-flex align-items-center tracking-tight text-dark">
                 <i class="bi bi-cpu text-primary me-3 fs-3 glow-text-primary"></i> Вектори Аналізу
               </h4>
               <p class="text-muted small mb-0 fw-medium">Оберіть алгоритми для глибокого структурного та семантичного сканування.</p>
             </div>

             <div class="d-flex flex-wrap gap-4 mb-4 p-3 rounded-4 bg-white bg-opacity-50 border shadow-sm position-relative z-1">
                <div class="d-flex align-items-center bg-white px-3 py-2 rounded-pill shadow-sm border">
                  <div class="form-check form-switch m-0 pe-2" style="transform: scale(1.1); transform-origin: left;">
                    <input class="form-check-input shadow-sm cursor-pointer" type="checkbox" id="toggle-all-cats"
                           :checked="isAllMethodsSelected" @change="toggleAllMethods">
                  </div>
                  <label class="fw-bold text-dark small text-uppercase tracking-wider cursor-pointer mt-1" for="toggle-all-cats">
                    Увімкнути всі
                  </label>
                </div>
                <div class="d-flex align-items-center">
                  <div class="form-check form-switch m-0 pe-2">
                    <input class="form-check-input shadow-sm cursor-pointer" type="checkbox" id="param-comments" v-model="ignoreComments">
                  </div>
                  <label class="fw-bold text-dark small text-uppercase tracking-wider cursor-pointer mt-1" for="param-comments">
                    <i class="bi bi-chat-left-text text-primary me-2 opacity-75"></i>Ігнорувати коментарі
                  </label>
                </div>
                <div class="d-flex align-items-center">
                  <div class="form-check form-switch m-0 pe-2">
                    <input class="form-check-input shadow-sm cursor-pointer" type="checkbox" id="param-whitespace" v-model="ignoreWhitespace">
                  </div>
                  <label class="fw-bold text-dark small text-uppercase tracking-wider cursor-pointer mt-1" for="param-whitespace">
                    <i class="bi bi-text-paragraph text-primary me-2 opacity-75"></i>Ігнорувати пробіли (та відступи)
                  </label>
                </div>
             </div>

             <div class="analysis-grid position-relative z-1">
               <div v-for="cat in analysisConfig" :key="cat.id" class="analysis-item">
                  <div class="category-card rounded-4 p-4 h-100 d-flex flex-column position-relative overflow-hidden shadow-sm">

                     <div class="d-flex justify-content-between align-items-start mb-4">
                        <div class="d-flex align-items-center">
                           <div class="p-2 bg-white rounded-circle shadow-sm me-3 text-primary d-flex align-items-center justify-content-center" style="width: 45px; height: 45px;">
                             <i class="bi fs-4" :class="cat.icon"></i>
                           </div>
                           <span class="fw-black text-dark text-uppercase tracking-wider" style="font-size: 0.85rem;">
                             {{ cat.name }}
                           </span>
                        </div>
                        <div class="form-check form-switch m-0" style="transform: scale(1.1); transform-origin: right;">
                          <input class="form-check-input shadow-sm cursor-pointer" type="checkbox" :id="'cat-' + cat.id"
                            :checked="selectedMethods[cat.id]?.length === cat.methods.length"
                            @change="(e: Event) => {
                              if((e.target as HTMLInputElement).checked) selectedMethods[cat.id] = cat.methods.map(m => m.value);
                              else delete selectedMethods[cat.id];
                            }">
                        </div>
                     </div>

                     <div class="d-flex flex-column gap-2 flex-grow-1 mt-auto">
                        <div v-for="method in cat.methods" :key="method.value">
                           <input type="checkbox" class="method-checkbox" :id="'m-' + cat.id + '-' + method.value"
                             :value="method.value" :checked="selectedMethods[cat.id]?.includes(method.value)"
                             @change="toggleMethod(cat.id, method.value)">
                           <label class="method-label w-100 shadow-sm" :for="'m-' + cat.id + '-' + method.value">
                              <i class="bi bi-check-circle-fill text-muted opacity-25 me-3 fs-5 transition-all"></i>
                              <span class="flex-grow-1 text-truncate">{{ method.label }}</span>
                           </label>
                        </div>
                     </div>
                  </div>
               </div>
             </div>
      </div>

      <div class="glass-card init-card p-4 p-md-5 position-relative overflow-hidden shadow-lg d-flex flex-column flex-lg-row align-items-center justify-content-between z-1 animate__fadeIn" style="animation-delay: 0.3s;">
         <div class="d-flex flex-column flex-lg-row align-items-center mb-4 mb-lg-0 text-center text-lg-start">
           <div class="d-flex align-items-center justify-content-center p-4 rounded-circle bg-primary bg-opacity-10 me-0 me-lg-4 mb-3 mb-lg-0 shadow-sm" style="width: 80px; height: 80px;">
             <i class="bi bi-rocket-takeoff text-primary fs-1 glow-text-primary" style="opacity: 0.9;"></i>
           </div>
           <div>
             <h3 class="fw-black mb-2 tracking-tight text-dark">Ініціалізація Ядра</h3>
             <p class="text-secondary mb-0 fw-medium small px-2 px-lg-0">Конфігурація векторів налаштована. Система готова до глибокого сканування зв'язків.</p>
           </div>
         </div>

         <button
           class="btn magic-btn btn-lg py-3 px-5 rounded-pill d-flex align-items-center justify-content-center gap-3 shadow-lg hover-lift"
           @click="startComparison"
           :disabled="isLoading ||
             (inputMode === 'file' && (!file1 || !file2)) ||
             (inputMode === 'text' && (!code1 || !code2)) ||
             (inputMode === 'storage' && ((storageLeftMode === 'file' && !file1) || (storageLeftMode === 'text' && !code1) || selectedStorageIds.length === 0))"
           style="min-width: 320px;"
         >
           <span v-if="isLoading" class="spinner-border spinner-border-sm"></span>
           <i class="bi bi-lightning-charge-fill fs-4" v-if="!isLoading"></i>
           <span class="fw-black fs-5 tracking-wider" style="letter-spacing: 1px;">{{ isLoading ? 'АНАЛІЗУЄМО...' : 'ЗАПУСТИТИ ЯДРО' }}</span>
         </button>
      </div>

    </div>
  </div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Fira+Code:wght@400;500;700&family=Inter:wght@300;400;500;600;700;800;900&display=swap');

.dashboard-bg {
  background: #f8f9fa;
  background-image:
    radial-gradient(at 0% 0%, rgba(13, 110, 253, 0.08) 0px, transparent 50%),
    radial-gradient(at 100% 0%, rgba(220, 53, 69, 0.04) 0px, transparent 50%),
    radial-gradient(at 50% 100%, rgba(16, 185, 129, 0.05) 0px, transparent 50%);
  color: #212529;
  font-family: 'Inter', system-ui, sans-serif;
  overflow-x: hidden;
}

.backdrop-blur {
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
}

.glass-card {
  background: rgba(255, 255, 255, 0.7);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  border: 1px solid rgba(255, 255, 255, 0.6);
  border-radius: 1.5rem;
  box-shadow: 0 10px 40px -10px rgba(0, 0, 0, 0.08);
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1), box-shadow 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
.glass-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 20px 50px -10px rgba(0, 0, 0, 0.12);
}

.glass-panel {
  background: rgba(255, 255, 255, 0.5);
  backdrop-filter: blur(12px);
  border: 1px solid rgba(255, 255, 255, 0.8);
  border-radius: 1.25rem;
  box-shadow: 0 4px 15px rgba(0,0,0,0.03);
}

.dropzone-active {
  background: rgba(13, 110, 253, 0.08) !important;
  border-color: rgba(13, 110, 253, 0.6) !important;
  box-shadow: inset 0 0 30px rgba(13, 110, 253, 0.15) !important;
  transform: scale(1.02);
}

.glow-text { text-shadow: 0 0 30px currentColor; }
.glow-text-primary { text-shadow: 0 0 25px rgba(13, 110, 253, 0.6); }
.tracking-wider { letter-spacing: 0.05em; }
.tracking-tight { letter-spacing: -0.03em; }
.fw-black { font-weight: 900; }
.hover-glow:hover { box-shadow: 0 0 20px rgba(13, 110, 253, 0.4); }

.btn-glass {
  background: transparent;
  border: none;
  color: #6c757d;
  transition: all 0.3s ease;
}
.btn-glass:hover {
  background: rgba(13, 110, 253, 0.05);
  color: #0d6efd;
}
.btn-glass.active {
  background: #0d6efd;
  color: white;
  box-shadow: 0 8px 20px rgba(13, 110, 253, 0.3);
}

.method-checkbox { display: none; }
.method-label {
  display: flex;
  align-items: center;
  padding: 0.85rem 1.25rem;
  background: rgba(255,255,255,0.7);
  border: 1px solid rgba(0,0,0,0.06);
  border-radius: 1rem;
  cursor: pointer;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
  font-size: 0.85rem;
  font-weight: 600;
  color: #495057;
}
.method-label:hover {
  background: #ffffff;
  transform: translateY(-2px);
  box-shadow: 0 5px 15px rgba(0,0,0,0.05);
}
.method-checkbox:checked + .method-label {
  background: rgba(13, 110, 253, 0.08);
  border-color: rgba(13, 110, 253, 0.3);
  color: #0d6efd;
  box-shadow: inset 0 0 0 1px rgba(13, 110, 253, 0.2);
}
.method-checkbox:checked + .method-label i {
  color: #10b981 !important;
  opacity: 1 !important;
  transform: scale(1.1);
  text-shadow: 0 0 15px rgba(16, 185, 129, 0.6);
}

.magic-btn {
  background: linear-gradient(135deg, #0d6efd, #3b82f6);
  border: none;
  color: white;
  text-transform: uppercase;
  letter-spacing: 2px;
  font-weight: 900;
  box-shadow: 0 15px 35px rgba(13, 110, 253, 0.4);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  overflow: hidden;
  z-index: 1;
}
.magic-btn::before {
  content: '';
  position: absolute;
  top: 0; left: 0; width: 100%; height: 100%;
  background: linear-gradient(135deg, #3b82f6, #0d6efd);
  z-index: -1;
  transition: opacity 0.3s ease;
  opacity: 0;
}
.magic-btn:hover {
  transform: translateY(-4px) scale(1.01);
  box-shadow: 0 20px 45px rgba(13, 110, 253, 0.5);
}
.magic-btn:hover::before { opacity: 1; }
.magic-btn:disabled {
  background: #ced4da;
  box-shadow: none;
  transform: none;
  cursor: not-allowed;
}

.category-card {
  transition: all 0.3s ease;
  border: 1px solid rgba(255,255,255,0.6);
  background: rgba(255,255,255,0.4);
}
.category-card:hover {
  background: rgba(255,255,255,0.8);
  box-shadow: 0 12px 30px rgba(0,0,0,0.06);
}

.analysis-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1.5rem;
}

.cursor-pointer { cursor: pointer; }
.hover-lift { transition: transform 0.2s ease, box-shadow 0.2s ease; }
.hover-lift:hover { transform: translateY(-2px); box-shadow: 0 10px 20px rgba(0,0,0,0.1) !important; }

.lang-option:hover { background: rgba(13, 110, 253, 0.05); padding-left: 1.25rem !important; }
.rotate-180 { transform: rotate(180deg); }
.fade-down-enter-active, .fade-down-leave-active { transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1); }
.fade-down-enter-from, .fade-down-leave-to { opacity: 0; transform: translateY(-10px); }
.fade-up-enter-active, .fade-up-leave-active { transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1); }
.fade-up-enter-from, .fade-up-leave-to { opacity: 0; transform: translateY(10px); }

.custom-scrollbar::-webkit-scrollbar { width: 6px; height: 6px; }
.custom-scrollbar::-webkit-scrollbar-track { background: rgba(0,0,0,0.02); border-radius: 4px; }
.custom-scrollbar::-webkit-scrollbar-thumb { background: rgba(13, 110, 253, 0.2); border-radius: 4px; }
.custom-scrollbar::-webkit-scrollbar-thumb:hover { background: rgba(13, 110, 253, 0.4); }

.animate__fadeIn { animation: fadeIn 0.6s cubic-bezier(0.4, 0, 0.2, 1) forwards; }
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(15px); }
  to { opacity: 1; transform: translateY(0); }
}

.form-switch .form-check-input {
  width: 3em; height: 1.5em;
  background-color: rgba(0,0,0,0.15);
  border: none;
  box-shadow: inset 0 1px 3px rgba(0,0,0,0.1);
}
.form-switch .form-check-input:checked { background-color: #0d6efd; box-shadow: inset 0 1px 3px rgba(0,0,0,0.2); }

.init-card {
  background: linear-gradient(135deg, rgba(255,255,255,0.9), rgba(255,255,255,0.6));
  border: 1px solid rgba(255,255,255,1);
}
</style>

<style>
/* Глобальные переопределения для элементов HomeView (не изолировано через scoped) */
[data-bs-theme="dark"] .category-card {
  background: rgba(30, 30, 30, 0.5) !important;
  border-color: rgba(255, 255, 255, 0.08) !important;
}
[data-bs-theme="dark"] .category-card:hover {
  background: rgba(40, 40, 40, 0.8) !important;
  box-shadow: 0 12px 30px rgba(0,0,0,0.3) !important;
}
[data-bs-theme="dark"] .method-label {
  background: rgba(40, 40, 40, 0.5) !important;
  border-color: rgba(255, 255, 255, 0.05) !important;
  color: #e0e0e0 !important;
}
[data-bs-theme="dark"] .method-label:hover {
  background: rgba(50, 50, 50, 0.9) !important;
  box-shadow: 0 5px 15px rgba(0, 0, 0, 0.3) !important;
}
[data-bs-theme="dark"] .method-checkbox:checked + .method-label {
  background: rgba(13, 110, 253, 0.15) !important;
  border-color: rgba(13, 110, 253, 0.3) !important;
  color: #8bb9fe !important;
}
[data-bs-theme="dark"] .init-card {
  background: linear-gradient(135deg, rgba(30, 30, 30, 0.9), rgba(30, 30, 30, 0.6)) !important;
  border-color: rgba(255, 255, 255, 0.1) !important;
}
[data-bs-theme="dark"] .form-switch .form-check-input {
  background-color: rgba(255, 255, 255, 0.15) !important;
  box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.5) !important;
}
[data-bs-theme="dark"] .form-switch .form-check-input:checked {
  background-color: #0d6efd !important;
}
</style>
