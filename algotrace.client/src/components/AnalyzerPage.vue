<template>
  <div class="vh-100 vw-100 d-flex flex-column bg-light overflow-hidden font-monospace" style="font-family: 'Inter', sans-serif;">

    <header class="d-flex align-items-center justify-content-between px-4 py-2 bg-white border-bottom shadow-sm flex-shrink-0" style="height: 60px; z-index: 10;">
      <div class="d-flex align-items-center gap-3">
        <button class="btn btn-light border-0 d-flex align-items-center justify-content-center" @click="$router.push('/')" style="width: 40px; height: 40px;">
          <i class="bi bi-arrow-left fs-5 text-secondary"></i>
        </button>

        <div class="d-flex align-items-center justify-content-center rounded-circle border border-3 fw-bold"
             :class="scoreClass" style="width: 42px; height: 42px; font-size: 13px;">
          {{ report.info.overall_score || 0 }}%
        </div>

        <div class="d-flex align-items-center">
          <div class="bg-primary bg-gradient text-white rounded p-1 me-2 d-flex align-items-center justify-content-center" style="width: 32px; height: 32px;">
            <i class="bi bi-layers-half"></i>
          </div>
          <div class="d-flex flex-column lh-1">
            <span class="fw-bolder text-dark" style="font-size: 16px; letter-spacing: -0.5px;">AlgoTrace</span>
            <span class="text-primary fw-bold" style="font-size: 9px; letter-spacing: 1px;">PRO ANALYZER</span>
          </div>
        </div>
      </div>

      <div class="d-flex gap-2">
        <span class="badge bg-light text-secondary border px-2 py-1">{{ report.info.mode }}</span>
        <span class="badge bg-light text-secondary border px-2 py-1">{{ report.info.date }}</span>
      </div>
    </header>

    <div class="d-flex flex-grow-1 overflow-hidden w-100">

      <FileTree
        title="Target Project"
        :items="report.submission_tree"
        :selectedFile="activeLeft?.name"
        @select="handleSelectLeft"
      />

      <div class="d-flex flex-column flex-grow-1 bg-white min-w-0">
        <div class="d-flex h-100 w-100 min-w-0">

          <div class="d-flex flex-column flex-grow-1 min-w-0">
            <div class="px-3 py-2 bg-light border-bottom text-secondary fw-bold d-flex align-items-center" style="font-size: 11px; height: 35px;">
              ANALYZED: <span class="fw-normal text-dark ms-1">{{ activeLeft?.name || 'Файл не обрано' }}</span>
            </div>
            <div class="d-flex flex-grow-1 position-relative overflow-hidden min-w-0 bg-white">
              <div class="flex-grow-1 overflow-auto py-2 position-relative w-100 custom-scrollbar" @scroll="syncLines">
                <div v-for="(line, idx) in leftLines" :key="'l'+idx"
                     :id="'l-line-' + (idx + 1)"
                     class="line-row" :class="getLineClass(idx + 1, 'left')">
                  <span class="ln">{{ idx + 1 }}</span>
                  <code class="lc" v-html="renderCodeLine(line, idx + 1, 'left')"></code>
                </div>
              </div>
              <div class="minimap" v-if="leftLines.length > 0">
                <div v-for="m in minimapLeft" :key="m.id" class="minimap-marker" :class="'bg-sev-' + m.severity" :style="{ top: m.top + '%', height: m.height + '%' }"></div>
              </div>
            </div>
          </div>

          <div class="connector-space bg-light border-start border-end">
            <svg class="connector-svg">
              <path v-for="link in visualLinks" :key="link.id" :d="link.d" class="link-path" :class="'stroke-' + link.severity" />
            </svg>
          </div>

          <div class="d-flex flex-column flex-grow-1 min-w-0">
            <div class="px-3 py-2 bg-light border-bottom text-secondary fw-bold d-flex align-items-center" style="font-size: 11px; height: 35px;">
              REFERENCE: <span class="fw-normal text-dark ms-1">{{ activeRight?.name || 'Еталон не обрано' }}</span>
            </div>
            <div class="d-flex flex-grow-1 position-relative overflow-hidden min-w-0 bg-white">
              <div class="flex-grow-1 overflow-auto py-2 position-relative w-100 custom-scrollbar" @scroll="syncLines">
                <div v-for="(line, idx) in rightLines" :key="'r'+idx"
                     :id="'r-line-' + (idx + 1)"
                     class="line-row" :class="getLineClass(idx + 1, 'right')">
                  <span class="ln">{{ idx + 1 }}</span>
                  <code class="lc" v-html="renderCodeLine(line, idx + 1, 'right')"></code>
                </div>
              </div>
              <div class="minimap" v-if="rightLines.length > 0">
                <div v-for="m in minimapRight" :key="m.id" class="minimap-marker" :class="'bg-sev-' + m.severity" :style="{ top: m.top + '%', height: m.height + '%' }"></div>
              </div>
            </div>
          </div>

        </div>
      </div>

      <FileTree
        title="Reference Library"
        :items="report.reference_tree"
        :selectedFile="activeRight?.name"
        :dynamicScores="activeLeft?.reference_scores"
        @select="handleSelectRight"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick, watch } from 'vue';
import FileTree from './FileTree.vue';

// --- 1. ОПИСАНИЕ ТИПОВ ДАННЫХ (ИНТЕРФЕЙСЫ) ---
interface Fragment {
  left_cols?: [number, number];
  right_cols?: [number, number];
}

interface Match {
  id: string | number;
  severity: string;
  left_lines: [number, number];
  right_lines: [number, number];
  fragments?: Fragment[];
}

interface FileNode {
  name: string;
  path: string;
  score?: number;
  reference_scores?: Record<string, number>;
  detailed_matches?: Record<string, Match[]>;
  type?: string;
  children?: FileNode[];
}

interface ReportInfo {
  overall_score?: number;
  mode?: string;
  date?: string;
}

interface ReportData {
  info: ReportInfo;
  submission_tree: FileNode[];
  reference_tree: FileNode[];
}

interface VisualLink {
  id: number;
  d: string;
  severity: string;
}

// --- 2. ИНИЦИАЛИЗАЦИЯ ПЕРЕМЕННЫХ С ТИПАМИ ---
const report = ref<ReportData>({ info: {}, submission_tree: [], reference_tree: [] });
const activeLeft = ref<FileNode | null>(null);
const activeRight = ref<FileNode | null>(null);
const leftContent = ref("");
const rightContent = ref("");
const visualLinks = ref<VisualLink[]>([]);

const leftLines = computed(() => leftContent.value ? leftContent.value.split('\n') : []);
const rightLines = computed(() => rightContent.value ? rightContent.value.split('\n') : []);

const scoreClass = computed(() => {
  const s = report.value.info?.overall_score || 0;
  return s > 75 ? 'border-danger text-danger' : s > 40 ? 'border-warning text-warning' : 'border-success text-success';
});

const currentMatches = computed<Match[]>(() => {
  if (!activeLeft.value || !activeRight.value) return [];
  return activeLeft.value.detailed_matches?.[activeRight.value.name] || [];
});

const syncLines = () => {
  const links: VisualLink[] = [];
  const headerHeight = 35;

  currentMatches.value.forEach((m: Match, index: number) => {
    const lEl = document.getElementById(`l-line-${m.left_lines[0]}`);
    const rEl = document.getElementById(`r-line-${m.right_lines[0]}`);

    if (lEl && rEl) {
      const lScroll = lEl.closest('.custom-scrollbar')?.scrollTop || 0;
      const rScroll = rEl.closest('.custom-scrollbar')?.scrollTop || 0;

      const lPos = lEl.offsetTop + (lEl.offsetHeight / 2) - lScroll + headerHeight;
      const rPos = rEl.offsetTop + (rEl.offsetHeight / 2) - rScroll + headerHeight;

      const d = `M 0 ${lPos} C 30 ${lPos}, 30 ${rPos}, 60 ${rPos}`;
      links.push({ id: index, d, severity: m.severity });
    }
  });
  visualLinks.value = links;
};

watch([leftContent, rightContent, currentMatches], () => nextTick(syncLines));
onMounted(() => window.addEventListener('resize', syncLines));

const calculateMinimap = (totalLines: number, side: 'left' | 'right') => {
  if (totalLines === 0 || currentMatches.value.length === 0) return [];
  return currentMatches.value.map((m: Match) => {
    const range = side === 'left' ? m.left_lines : m.right_lines;
    const top = (range[0] / totalLines) * 100;
    const height = Math.max(((range[1] - range[0] + 1) / totalLines) * 100, 1.5);
    return { id: String(m.id) + side, top, height, severity: m.severity };
  });
};

const minimapLeft = computed(() => calculateMinimap(leftLines.value.length, 'left'));
const minimapRight = computed(() => calculateMinimap(rightLines.value.length, 'right'));

const getLineClass = (lineNum: number, side: 'left' | 'right') => {
  const match = currentMatches.value.find((m: Match) => {
    const range = side === 'left' ? m.left_lines : m.right_lines;
    return lineNum >= range[0] && lineNum <= range[1];
  });
  return match && !match.fragments ? `bg-match sev-${match.severity}` : '';
};

const renderCodeLine = (lineStr: string, lineNum: number, side: 'left' | 'right') => {
  let html = lineStr;

  const match = currentMatches.value.find((m: Match) => {
    const range = side === 'left' ? m.left_lines : m.right_lines;
    return lineNum >= range[0] && lineNum <= range[1] && m.fragments;
  });

if (match && match.fragments && match.fragments.length > 0) {
    const frag = match.fragments[0];
    if (frag) {
      const cols = side === 'left' ? frag.left_cols : frag.right_cols;
      if (cols) {
        const before = html.substring(0, cols[0]);
        const target = html.substring(cols[0], cols[1]);
        const after = html.substring(cols[1]);
        html = `${before}<mark class="frag-match sev-${match.severity}">${target}</mark>${after}`;
      }
    }
  }

  html = html.replace(/\b(def|class|import|from|return|if|else|elif|for|while|try|except|const|let|var|function|export|static)\b/g, '<span class="syn-kw">$1</span>');
  html = html.replace(/(".*?"|'.*?')/g, '<span class="syn-str">$1</span>');
  html = html.replace(/\b(\d+)\b/g, '<span class="syn-num">$1</span>');

  return html;
};

const handleSelectLeft = async (file: FileNode) => {
  activeLeft.value = file;
  const res = await fetch(`/data/${file.path}`);
  leftContent.value = await res.text();
};

const handleSelectRight = async (file: FileNode) => {
  activeRight.value = file;
  const res = await fetch(`/data/${file.path}`);
  rightContent.value = await res.text();
};

onMounted(async () => {
  try {
    const res = await fetch('/data/report.json');
    report.value = await res.json();
  } catch (e) {
    console.error("Не вдалося завантажити звіт", e);
  }
});
</script>

<style scoped>
.min-w-0 { min-width: 0; }

.connector-space { width: 60px; position: relative; flex-shrink: 0; }
.connector-svg { position: absolute; top: 0; left: 0; width: 100%; height: 100%; pointer-events: none; }
.link-path { fill: none; stroke-width: 2; opacity: 0.6; transition: d 0.1s ease; }
.link-path:hover { opacity: 1; stroke-width: 3; }

.stroke-high { stroke: #dc3545; }
.stroke-med { stroke: #fd7e14; }
.stroke-low { stroke: #198754; }
.stroke-purple { stroke: #6f42c1; }
.stroke-blue { stroke: #0d6efd; }

.bg-sev-high { background: #dc3545; }
.bg-sev-med { background: #fd7e14; }
.bg-sev-low { background: #198754; }
.bg-sev-purple { background: #6f42c1; }
.bg-sev-blue { background: #0d6efd; }

.minimap { width: 6px; background: #f1f3f5; border-left: 1px solid #dee2e6; position: relative; z-index: 10; flex-shrink: 0; }
.minimap-marker { position: absolute; left: 0; width: 100%; opacity: 0.8; border-radius: 1px; }

.bg-match.sev-high { background: rgba(220, 53, 69, 0.1); border-left: 3px solid #dc3545; }
mark.frag-match.sev-high { background: rgba(220, 53, 69, 0.2); border-bottom: 2px solid #dc3545; color: #212529; }

.bg-match.sev-med { background: rgba(253, 126, 20, 0.1); border-left: 3px solid #fd7e14; }
mark.frag-match.sev-med { background: rgba(253, 126, 20, 0.2); border-bottom: 2px solid #fd7e14; color: #212529; }

.bg-match.sev-low { background: rgba(25, 135, 84, 0.1); border-left: 3px solid #198754; }
mark.frag-match.sev-low { background: rgba(25, 135, 84, 0.2); border-bottom: 2px solid #198754; color: #212529; }

.bg-match.sev-purple { background: rgba(111, 66, 193, 0.1); border-left: 3px solid #6f42c1; }
mark.frag-match.sev-purple { background: rgba(111, 66, 193, 0.2); border-bottom: 2px solid #6f42c1; color: #212529; }

.bg-match.sev-blue { background: rgba(13, 110, 253, 0.1); border-left: 3px solid #0d6efd; }
mark.frag-match.sev-blue { background: rgba(13, 110, 253, 0.2); border-bottom: 2px solid #0d6efd; color: #212529; }

.line-row {
  display: flex;
  padding: 0 10px;
  font-family: 'Fira Code', 'Courier New', monospace;
  font-size: 13px;
  line-height: 22px;
  border-left: 3px solid transparent;
  min-width: max-content;
}
.line-row:hover { background: rgba(0, 0, 0, 0.03); }

.ln { width: 40px; text-align: right; padding-right: 15px; color: #adb5bd; font-size: 11px; user-select: none; }
.lc { white-space: pre; color: #24292e; }
mark.frag-match { border-radius: 3px; padding: 0 2px; }

.syn-kw { color: #0000ff; font-weight: bold; }
.syn-str { color: #a31515; }
.syn-num { color: #098658; }

.custom-scrollbar::-webkit-scrollbar { width: 10px; height: 10px; }
.custom-scrollbar::-webkit-scrollbar-corner { background: #f8f9fa; }
.custom-scrollbar::-webkit-scrollbar-thumb { background: #adb5bd; border-radius: 5px; border: 2px solid #ffffff; }
.custom-scrollbar::-webkit-scrollbar-thumb:hover { background: #6c757d; }
</style>
