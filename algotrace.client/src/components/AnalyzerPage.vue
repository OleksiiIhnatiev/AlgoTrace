<template>
  <div class="trace-app">
    <header class="trace-header shadow-sm">
      <div class="header-left">
        <div class="overall-badge" :class="scoreClass">{{ report.info.overall_score || 0 }}%</div>
        <div class="brand d-flex align-items-center">
          <div class="bg-primary bg-gradient text-white rounded-3 p-1 me-2 d-flex align-items-center justify-content-center" style="width: 28px; height: 28px;">
            <i class="bi bi-layers-half fs-6"></i>
          </div>
          <div class="d-flex flex-column lh-1">
            <span class="brand-main">AlgoTrace</span>
            <span class="brand-sub">PRO ANALYZER</span>
          </div>
        </div>
      </div>
      <div class="header-right">
        <div class="tag">{{ report.info.mode }}</div>
        <div class="tag">{{ report.info.date }}</div>
      </div>
    </header>

    <div class="trace-workspace">
      <FileTree 
        title="Target Project" 
        :items="report.submission_tree" 
        :selectedFile="activeLeft?.name" 
        @select="handleSelectLeft" 
      />

      <div class="trace-viewer">
        <div class="comparison-grid">
          
          <div class="code-column">
            <div class="col-header">ANALYZED: <span class="fw-normal ms-1 text-dark">{{ activeLeft?.name || 'Файл не обрано' }}</span></div>
            <div class="code-container">
              <div class="scroll-area" @scroll="syncLines">
                <div v-for="(line, idx) in leftLines" :key="'l'+idx" 
                     :id="'l-line-' + (idx + 1)"
                     class="line-row" :class="getLineClass(idx + 1, 'left')">
                  <span class="ln">{{ idx + 1 }}</span>
                  <code class="lc" v-html="renderCodeLine(line, idx + 1, 'left')"></code>
                </div>
              </div>
              <div class="minimap" v-if="leftLines.length > 0">
                <div v-for="m in minimapLeft" :key="m.id" 
                     class="minimap-marker" 
                     :class="'marker-' + m.severity"
                     :style="{ top: m.top + '%', height: m.height + '%' }">
                </div>
              </div>
            </div>
          </div>

          <div class="connector-space">
            <svg class="connector-svg">
              <path v-for="link in visualLinks" :key="link.id" 
                    :d="link.d" 
                    class="link-path"
                    :class="'stroke-' + link.severity" />
            </svg>
          </div>

          <div class="code-column">
            <div class="col-header">REFERENCE: <span class="fw-normal ms-1 text-dark">{{ activeRight?.name || 'Еталон не обрано' }}</span></div>
            <div class="code-container">
              <div class="scroll-area" @scroll="syncLines">
                <div v-for="(line, idx) in rightLines" :key="'r'+idx" 
                     :id="'r-line-' + (idx + 1)"
                     class="line-row" :class="getLineClass(idx + 1, 'right')">
                  <span class="ln">{{ idx + 1 }}</span>
                  <code class="lc" v-html="renderCodeLine(line, idx + 1, 'right')"></code>
                </div>
              </div>
              <div class="minimap" v-if="rightLines.length > 0">
                <div v-for="m in minimapRight" :key="m.id" 
                     class="minimap-marker" 
                     :class="'marker-' + m.severity"
                     :style="{ top: m.top + '%', height: m.height + '%' }">
                </div>
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

<script setup>
import { ref, computed, onMounted, nextTick, watch } from 'vue';
import FileTree from './FileTree.vue';

const report = ref({ info: {}, submission_tree: [], reference_tree: [] });
const activeLeft = ref(null);
const activeRight = ref(null);
const leftContent = ref("");
const rightContent = ref("");
const visualLinks = ref([]);

const leftLines = computed(() => leftContent.value ? leftContent.value.split('\n') : []);
const rightLines = computed(() => rightContent.value ? rightContent.value.split('\n') : []);

const scoreClass = computed(() => {
  const s = report.value.info?.overall_score || 0;
  return s > 75 ? 'bad' : s > 40 ? 'warn' : 'good';
});

const currentMatches = computed(() => {
  if (!activeLeft.value || !activeRight.value) return [];
  return activeLeft.value.detailed_matches?.[activeRight.value.name] || [];
});

const syncLines = () => {
  const links = [];
  const headerHeight = 35; 

  currentMatches.value.forEach((m, index) => {
    const lEl = document.getElementById(`l-line-${m.left_lines[0]}`);
    const rEl = document.getElementById(`r-line-${m.right_lines[0]}`);

    if (lEl && rEl) {
      const lScroll = lEl.closest('.scroll-area').scrollTop;
      const rScroll = rEl.closest('.scroll-area').scrollTop;

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

const calculateMinimap = (totalLines, side) => {
  if (totalLines === 0 || currentMatches.value.length === 0) return [];
  return currentMatches.value.map(m => {
    const range = side === 'left' ? m.left_lines : m.right_lines;
    const top = (range[0] / totalLines) * 100;
    const height = Math.max(((range[1] - range[0] + 1) / totalLines) * 100, 1.5);
    return { id: m.id + side, top, height, severity: m.severity };
  });
};

const minimapLeft = computed(() => calculateMinimap(leftLines.value.length, 'left'));
const minimapRight = computed(() => calculateMinimap(rightLines.value.length, 'right'));

const getLineClass = (lineNum, side) => {
  const match = currentMatches.value.find(m => {
    const range = side === 'left' ? m.left_lines : m.right_lines;
    return lineNum >= range[0] && lineNum <= range[1];
  });
  return match && !match.fragments ? `bg-match sev-${match.severity}` : '';
};

const renderCodeLine = (lineStr, lineNum, side) => {
  let html = lineStr;

  const match = currentMatches.value.find(m => {
    const range = side === 'left' ? m.left_lines : m.right_lines;
    return lineNum >= range[0] && lineNum <= range[1] && m.fragments;
  });

  if (match && match.fragments.length > 0) {
    const frag = match.fragments[0];
    const cols = side === 'left' ? frag.left_cols : frag.right_cols;
    if (cols) {
      const before = html.substring(0, cols[0]);
      const target = html.substring(cols[0], cols[1]);
      const after = html.substring(cols[1]);
      html = `${before}<mark class="frag-match sev-${match.severity}">${target}</mark>${after}`;
    }
  }

  html = html.replace(/\b(def|class|import|from|return|if|else|elif|for|while|try|except|const|let|var|function|export|static)\b/g, '<span class="syn-kw">$1</span>');
  html = html.replace(/(".*?"|'.*?')/g, '<span class="syn-str">$1</span>');
  html = html.replace(/\b(\d+)\b/g, '<span class="syn-num">$1</span>');

  return html;
};

const handleSelectLeft = async (file) => {
  activeLeft.value = file;
  const res = await fetch(`/data/${file.path}`);
  leftContent.value = await res.text();
};

const handleSelectRight = async (file) => {
  activeRight.value = file;
  const res = await fetch(`/data/${file.path}`);
  rightContent.value = await res.text();
};

onMounted(async () => {
  const res = await fetch('/data/report.json');
  report.value = await res.json();
});
</script>

<style scoped>
.trace-app {
  display: flex; flex-direction: column;
  height: 100vh; width: 100vw;
  background: #f8f9fa; color: #212529;
  overflow: hidden; font-family: 'Inter', sans-serif;
}

.trace-header { height: 60px; background: #ffffff; border-bottom: 1px solid #dee2e6; display: flex; align-items: center; justify-content: space-between; padding: 0 20px; flex-shrink: 0; z-index: 10; }
.header-left { display: flex; align-items: center; gap: 15px; }
.overall-badge { width: 40px; height: 40px; border-radius: 50%; border: 3px solid; display: flex; align-items: center; justify-content: center; font-weight: bold; font-size: 12px; }
.overall-badge.bad { border-color: #dc3545; color: #dc3545; }
.overall-badge.warn { border-color: #fd7e14; color: #fd7e14; }
.overall-badge.good { border-color: #198754; color: #198754; }
.brand-main { font-weight: 800; color: #212529; font-size: 16px; letter-spacing: -0.5px; }
.brand-sub { font-size: 9px; color: #0d6efd; letter-spacing: 1px; font-weight: 600; }
.header-right { display: flex; gap: 10px; }
.tag { background: #e9ecef; padding: 4px 10px; border-radius: 6px; font-size: 10px; color: #495057; text-transform: uppercase; font-weight: 600; border: 1px solid #dee2e6; }

.trace-workspace { flex: 1; display: flex; overflow: hidden; width: 100%; }
.trace-viewer { flex: 1; display: flex; flex-direction: column; background: #ffffff; min-width: 0; }
.comparison-grid { display: flex; height: 100%; width: 100%; min-width: 0; }

.code-column { flex: 1; display: flex; flex-direction: column; min-width: 0; }
.col-header { height: 35px; background: #f1f3f5; border-bottom: 1px solid #dee2e6; font-size: 11px; display: flex; align-items: center; padding: 0 15px; color: #6c757d; font-weight: bold; }
.code-container { flex: 1; display: flex; position: relative; overflow: hidden; min-width: 0; background: #ffffff; }

.scroll-area { flex: 1; overflow: auto; padding: 10px 0; position: relative; width: 100%; }

.connector-space { width: 60px; background: #f8f9fa; border-left: 1px solid #dee2e6; border-right: 1px solid #dee2e6; position: relative; flex-shrink: 0; }
.connector-svg { position: absolute; top: 0; left: 0; width: 100%; height: 100%; pointer-events: none; }
.link-path { fill: none; stroke-width: 2; opacity: 0.6; transition: d 0.1s ease; }
.link-path:hover { opacity: 1; stroke-width: 3; }

.stroke-high { stroke: #dc3545; }
.stroke-med { stroke: #fd7e14; }
.stroke-low { stroke: #198754; }
.stroke-purple { stroke: #6f42c1; }
.stroke-blue { stroke: #0d6efd; }

.minimap { width: 6px; background: #f1f3f5; border-left: 1px solid #dee2e6; position: relative; z-index: 10; flex-shrink: 0; }
.minimap-marker { position: absolute; left: 0; width: 100%; opacity: 0.8; border-radius: 1px; }

.bg-match.sev-high { background: rgba(220, 53, 69, 0.1); border-left: 3px solid #dc3545; }
mark.frag-match.sev-high { background: rgba(220, 53, 69, 0.2); border-bottom: 2px solid #dc3545; color: #212529; }
.marker-high { background: #dc3545; }

.bg-match.sev-med { background: rgba(253, 126, 20, 0.1); border-left: 3px solid #fd7e14; }
mark.frag-match.sev-med { background: rgba(253, 126, 20, 0.2); border-bottom: 2px solid #fd7e14; color: #212529; }
.marker-med { background: #fd7e14; }

.bg-match.sev-low { background: rgba(25, 135, 84, 0.1); border-left: 3px solid #198754; }
mark.frag-match.sev-low { background: rgba(25, 135, 84, 0.2); border-bottom: 2px solid #198754; color: #212529; }
.marker-low { background: #198754; }

.bg-match.sev-purple { background: rgba(111, 66, 193, 0.1); border-left: 3px solid #6f42c1; }
mark.frag-match.sev-purple { background: rgba(111, 66, 193, 0.2); border-bottom: 2px solid #6f42c1; color: #212529; }
.marker-purple { background: #6f42c1; }

.bg-match.sev-blue { background: rgba(13, 110, 253, 0.1); border-left: 3px solid #0d6efd; }
mark.frag-match.sev-blue { background: rgba(13, 110, 253, 0.2); border-bottom: 2px solid #0d6efd; color: #212529; }
.marker-blue { background: #0d6efd; }

.line-row { display: flex; padding: 0 10px; font-family: 'Fira Code', 'Courier New', monospace; font-size: 13px; line-height: 22px; border-left: 3px solid transparent; min-width: max-content; }
.line-row:hover { background: rgba(0, 0, 0, 0.03); }

.ln { width: 40px; text-align: right; padding-right: 15px; color: #adb5bd; font-size: 11px; user-select: none; }
.lc { white-space: pre; color: #24292e; } 
mark.frag-match { border-radius: 3px; padding: 0 2px; }

.syn-kw { color: #0000ff; font-weight: bold; }
.syn-str { color: #a31515; }
.syn-num { color: #098658; }

::-webkit-scrollbar { width: 10px; height: 10px; }
::-webkit-scrollbar-corner { background: #f8f9fa; }
::-webkit-scrollbar-thumb { background: #adb5bd; border-radius: 5px; border: 2px solid #ffffff; }
::-webkit-scrollbar-thumb:hover { background: #6c757d; }
</style>