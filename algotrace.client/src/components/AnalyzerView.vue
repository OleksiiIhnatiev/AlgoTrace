<script setup lang="ts">
import { computed, onMounted, ref, shallowRef, watch, nextTick } from 'vue';
import { useRouter } from 'vue-router';
import { analysisState } from '@/services/analysis.service';
import type { editor } from 'monaco-editor';
import InteractiveGraph from './InteractiveGraph.vue';
import { VueMonacoEditor } from '@guolao/vue-monaco-editor';
import type * as monaco from 'monaco-editor';
import { use } from 'echarts/core';
import { RadarChart } from 'echarts/charts';
import { TitleComponent, TooltipComponent, LegendComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import VChart from 'vue-echarts';
import type { Node as VisNode, Edge as VisEdge } from 'vis-network';
import { isDarkMode, toggleTheme } from '../composables/useTheme';
import ReportExportView from './ReportExportView.vue';

interface MatchedBlock {
  start_line_a: number;
  end_line_a: number;
  start_line_b: number;
  end_line_b: number;
  content_a: string;
  content_b: string;
  file_a?: string;
  file_b?: string;
}

interface MatchedHash {
  hash_value: string;
  token_sequence: string;
  occurrences: { submission: 'a' | 'b' }[];
}

interface ASTMatch {
  type: string;
  severity: string;
  leftLines: number[];
  rightLines: number[];
}

interface ASTNode {
  id: string;
  label: string;
  children?: string[];
}

interface SubtreeMatch {
  node_type: string;
  nodes_a: ASTNode[];
  nodes_b: ASTNode[];
}

interface GraphMatch {
  type: string;
  severity: string;
  left_lines: number[];
  right_lines: number[];
}

interface CFGNode {
  id: string;
  line: number;
  content: string;
  type: string;
}

interface CFGEdge {
  source: string;
  target: string;
  type?: string;
}

interface CFGGraph {
  nodes: CFGNode[];
  edges: CFGEdge[];
}

interface Evidence {
  matched_blocks?: MatchedBlock[];
  matched_hashes?: MatchedHash[];
  matched_subtrees?: SubtreeMatch[];
  matches?: GraphMatch[];
  graph_a?: CFGGraph;
  graph_b?: CFGGraph;
  metrics_a?: Record<string, number>;
  metrics_b?: Record<string, number>;
  conclusion?: string;
  length?: number;
  [index: number]: ASTMatch;
}

interface Algorithm {
  method: string;
  similarity_score: number;
  evidence_type: 'text_highlight' | 'token_sequence' | 'ast_tree_mapping' | 'graph_mapping' | 'metric_comparison';
  evidence: Evidence;
}

interface Category {
  category_name: string;
  category_similarity_score: number;
  algorithms: Algorithm[];
}

interface SourceFiles {
  name_a: string;
  file_a: string;
  name_b: string;
  file_b: string;
}

interface Report {
  analysis_id: string;
  global_similarity_score: number;
  categories_results: Category[];
  source_files: SourceFiles;
  language: string;
  error?: string | null;
}

interface DocumentComparisonResult {
  document_id?: string;
  target_file?: { filename: string; content: string };
  global_similarity_score: number;
  categories_results?: Category[];
  error?: string | null;
}

interface MultiReport {
  analysis_id: string;
  main_submission?: { filename: string; content: string };
  results: DocumentComparisonResult[];
  language?: string;
}

type RawReport = Report | MultiReport;

use([TitleComponent, TooltipComponent, LegendComponent, RadarChart, CanvasRenderer]);

const router = useRouter();
const activeCategory = ref<string | null>(null);
const activeAlgorithm = ref<Algorithm | null>(null);

const showExportModal = ref(false);
const exportSelections = ref<Record<string, boolean>>({});
const isGeneratingPdf = ref(false);
const exportSelectedMethods = ref<string[]>([]);

const isMultiReport = computed(() => {
  const raw = analysisState.currentReport as unknown as RawReport | null;
  return raw && 'results' in raw && Array.isArray(raw.results);
});

const multiResults = computed(() => {
  if (isMultiReport.value) {
    return (analysisState.currentReport as unknown as MultiReport).results;
  }
  return [];
});

const selectedResultIndex = ref(0);

const report = computed<Report | null>(() => {
  const raw = analysisState.currentReport as unknown as RawReport | null;
  if (!raw) return null;

  if (isMultiReport.value) {
    const multiRaw = raw as MultiReport;
    const result = multiRaw.results[selectedResultIndex.value];
    if (!result) return null;
    return {
      analysis_id: multiRaw.analysis_id,
      global_similarity_score: result.global_similarity_score,
      categories_results: result.categories_results || [],
      source_files: {
        name_a: multiRaw.main_submission?.filename || 'Файл 1',
        file_a: multiRaw.main_submission?.content || '',
        name_b: result.target_file?.filename || 'Файл 2',
        file_b: result.target_file?.content || ''
      },
      language: multiRaw.language || 'python',
      error: result.error || null
    } as Report;
  }

  return raw as Report;
});

watch(selectedResultIndex, () => {
  const rep = report.value;
  if (rep && rep.categories_results && rep.categories_results.length > 0) {
    const firstCat = rep.categories_results[0];
    if (firstCat) selectCategory(firstCat);
  } else {
    activeCategory.value = null;
    activeAlgorithm.value = null;
  }
});

onMounted(() => {
  if (!analysisState.currentReport) {
    router.push('/');
    return;
  }
  const rep = report.value;
  if (rep && rep.categories_results && rep.categories_results.length > 0) {
    const firstCat = rep.categories_results[0];
    if (firstCat) selectCategory(firstCat);
  }
});

const openExportModal = () => {
  if (!report.value) return;
  exportSelections.value = {};
  report.value.categories_results.forEach(cat => {
    cat.algorithms.forEach(algo => {
      exportSelections.value[algo.method] = true;
    });
  });
  showExportModal.value = true;
};

const confirmExport = () => {
  const selected = Object.keys(exportSelections.value).filter(k => exportSelections.value[k]);
  if (selected.length === 0) {
    alert('Оберіть хоча б один алгоритм для експорту');
    return;
  }
  exportSelectedMethods.value = selected;
  isGeneratingPdf.value = true;
};

const onPdfGenerated = () => {
  isGeneratingPdf.value = false;
  showExportModal.value = false;
};

const globalScore = computed(() => {
  return report.value?.global_similarity_score ? Math.round(report.value.global_similarity_score * 100) : 0;
});

const getScoreColorHex = (score: number) => {
  if (score < 30) return '#10b981';
  if (score < 70) return '#f59e0b';
  return '#ef4444';
};

const getBgColor = (score: number) => {
  if (score < 30) return 'bg-success bg-opacity-25 text-success border border-success border-opacity-50';
  if (score < 70) return 'bg-warning bg-opacity-25 text-warning border border-warning border-opacity-50';
  return 'bg-danger bg-opacity-25 text-danger border border-danger border-opacity-50';
};

const formatScore = (score: number) => Math.round((score || 0) * 100) + '%';

const formatCategoryName = (name: string) => {
  const map: Record<string, string> = {
    'text_based': 'Текстовий аналіз',
    'token_based': 'Токенний аналіз',
    'tree_based': 'Аналіз AST-дерев',
    'graph_based': 'Аналіз графів (CFG/PDG)',
    'metrics_based': 'Метрики коду'
  };
  return map[name] || name;
};

const formatMethodName = (method: string) => {
  const map: Record<string, string> = {
    'levenshtein': 'Відстань Левенштейна',
    'line_matching': 'Порядкове Порівняння',
    'rabin_karp': 'Алгоритм Рабіна-Карпа',
    'ngram_search': 'Пошук за N-грамами',
    'jaccard_token': 'Токени Джаккарда',
    'winnowing': 'Вінновінг (Winnowing)',
    'ast_hashing': 'Хешування AST',
    'ast_compare': 'Пряме Порівняння AST',
    'subtree_isomorphism': 'Ізоморфізм Піддерев',
    'cfg': 'Граф потоку керування (CFG)',
    'pdg': 'Граф залежностей даних (PDG)',
    'subgraph_isomorphism': 'Ізоморфізм підграфів',
    'halstead': 'Метрики Холстеда',
    'mccabe': 'Складність Маккейба'
  };
  return map[method] || method.replace(/_/g, ' ');
};

const formatEvidenceType = (type: string) => {
  const map: Record<string, string> = {
    'text_highlight': 'Виділення тексту',
    'token_sequence': 'Послідовність токенів',
    'ast_tree_mapping': 'Відображення AST-дерева',
    'graph_mapping': 'Відображення графа',
    'metric_comparison': 'Порівняння метрик'
  };
  return map[type] || type;
};

const formatSeverity = (severity: string) => {
  const map: Record<string, string> = {
    'high': 'Високий',
    'med': 'Середній',
    'low': 'Низький'
  };
  return map[severity] || severity;
};

const formatMatchType = (type: string) => {
  const map: Record<string, string> = {
    'Identical Subtree Found': 'Знайдено ідентичне піддерево',
    'Full Structure Match': 'Повний структурний збіг',
    'CFG Node Match': 'Збіг вузлів CFG',
    'Data Dependency Match': 'Збіг залежностей даних'
  };
  return map[type] || type;
};

const formatNodeType = (type: string) => {
  const map: Record<string, string> = {
    'statement': 'Інструкція',
    'control': 'Керування',
    'def': 'Визначення'
  };
  return map[type] || type;
};

const formatTokenSequence = (seq: string) => {
  const map: Record<string, string> = {
    'Token Sequence Match': 'Збіг послідовності токенів',
    'Token Vocabulary Similarity': 'Схожість словника токенів'
  };
  return map[seq] || seq;
};

const formatMetricKey = (key: string) => {
  const map: Record<string, string> = {
    'cyclomatic_complexity': 'Цикломатична складність',
    'complexity': 'Цикломатична складність',
    'halstead_effort': 'Зусилля (Halstead)',
    'halstead_volume': 'Об\'єм (Halstead)',
    'volume': 'Об\'єм (Halstead)',
    'halstead_difficulty': 'Складність (Halstead)',
    'difficulty': 'Складність (Halstead)',
    'halstead_bugs': 'Очікувані помилки (Halstead)',
    'maintainability_index': 'Індекс підтримуваності',
    'loc': 'Кількість рядків коду (LOC)'
  };
  return map[key] || String(key).replace(/_/g, ' ');
};

const selectCategory = (cat: Category) => {
  activeCategory.value = cat.category_name;
  if (cat.algorithms && cat.algorithms.length > 0) {
    activeAlgorithm.value = cat.algorithms[0] || null;
  } else {
    activeAlgorithm.value = null;
  }
};

const selectAlgorithm = (cat: Category, algo: Algorithm) => {
  activeCategory.value = cat.category_name;
  activeAlgorithm.value = algo;
};

const aggregateMatches = (matches: unknown) => {
  if (!matches || !Array.isArray(matches)) return [];
  const counts: Record<string, { type: string, severity: string, count: number }> = {};
  (matches as (ASTMatch | GraphMatch)[]).forEach((m) => {
    const key = m.type + '_' + m.severity;
    if (!counts[key]) counts[key] = { type: m.type, severity: m.severity, count: 0 };
    counts[key].count++;
  });
  return Object.values(counts);
};

const getLinesContent = (code: string | undefined, lines: number[] | undefined, type: string) => {
  if (!code || !lines || lines.length === 0) return '';
  const codeLines = code.split('\n');
  const result: string[] = [];

  const start = lines[0];
  const end = lines[1];
  if (type === 'ast_tree_mapping' && lines.length === 2 && start !== undefined && end !== undefined && start !== end) {
     for (let i = start; i <= end; i++) {
        const lineContent = codeLines[i - 1];
        if (lineContent) result.push(`${i}: ${lineContent.trim()}`);
     }
  } else {
     const uniqueLines = [...new Set(lines)].sort((a, b) => a - b);
     uniqueLines.forEach(l => {
       const lineContent = codeLines[l - 1];
       if (lineContent) result.push(`${l}: ${lineContent.trim()}`);
     });
  }
  return result.join('\n');
};

const editorRefA = shallowRef();
const editorRefB = shallowRef();
let monacoInstance: typeof monaco | null = null;
let decorationsCollectionA: editor.IEditorDecorationsCollection | null = null;
let decorationsCollectionB: editor.IEditorDecorationsCollection | null = null;

const handleEditorMount = (editor: editor.IStandaloneCodeEditor, m: typeof monaco, fileId: 'a' | 'b') => {
  if (fileId === 'a') editorRefA.value = editor;
  else editorRefB.value = editor;
  monacoInstance = m;
  updateMonacoDecorations();
};

const getHighlightedLines = (fileId: 'a' | 'b', evidenceType: string, evidenceData: Evidence | undefined) => {
  const lines = new Set<number>();
  if (!evidenceData) return [];
    if (evidenceType === 'text_highlight' && evidenceData.matched_blocks) {
      evidenceData.matched_blocks.forEach((match: MatchedBlock) => {
        const start = fileId === 'a' ? match.start_line_a : match.start_line_b;
        const end = fileId === 'a' ? match.end_line_a : match.end_line_b;
      for (let i = start; i <= end; i++) lines.add(i);
      });
    } else if (evidenceType === 'graph_mapping' && evidenceData.matches) {
       evidenceData.matches.forEach((match: GraphMatch) => {
         const matchLines = fileId === 'a' ? match.left_lines : match.right_lines;
       if (matchLines && Array.isArray(matchLines)) matchLines.forEach((l: number) => lines.add(l));
       });
    } else if (evidenceType === 'ast_tree_mapping' && Array.isArray(evidenceData)) {
       (evidenceData as ASTMatch[]).forEach((match: ASTMatch) => {
         const matchLines = fileId === 'a' ? match.leftLines : match.rightLines;
         if (matchLines && Array.isArray(matchLines)) {
           if (matchLines.length === 2 && matchLines[0] !== undefined && matchLines[1] !== undefined && matchLines[0] !== matchLines[1]) {
            for (let i = matchLines[0]; i <= matchLines[1]; i++) lines.add(i);
         } else {
            matchLines.forEach((l: number) => lines.add(l));
         }
         }
       });
    }
  return Array.from(lines);
};

const updateMonacoDecorations = () => {
  if (!editorRefA.value || !editorRefB.value || !activeAlgorithm.value || !monacoInstance) return;
  const algo = activeAlgorithm.value;
  const m = monacoInstance;

  const linesA = getHighlightedLines('a', algo.evidence_type, algo.evidence);
  const linesB = getHighlightedLines('b', algo.evidence_type, algo.evidence);

  const createRanges = (lines: number[]) => lines.map(l => ({
    range: new m.Range(l, 1, l, 1),
    options: { isWholeLine: true, className: 'plagiarism-highlight', marginClassName: 'plagiarism-margin' }
  }));

  if (decorationsCollectionA) decorationsCollectionA.clear();
  if (decorationsCollectionB) decorationsCollectionB.clear();

  decorationsCollectionA = editorRefA.value.createDecorationsCollection(createRanges(linesA));
  decorationsCollectionB = editorRefB.value.createDecorationsCollection(createRanges(linesB));
};

watch(activeAlgorithm, () => nextTick(updateMonacoDecorations));

const buildASTVisData = (nodes: ASTNode[]): { nodes: VisNode[], edges: VisEdge[] } => {
  const visNodes: VisNode[] = [];
  const visEdges: VisEdge[] = [];
  if (!nodes) return { nodes: visNodes, edges: visEdges };
  nodes.forEach((n: ASTNode) => {
    visNodes.push({ id: n.id, label: (n.label || '').replace(/[\[\]{}()<>]/g, ' ') });
    if (n.children && Array.isArray(n.children)) {
      n.children.forEach((cId: string) => visEdges.push({ from: n.id, to: cId }));
    }
  });
  return { nodes: visNodes, edges: visEdges };
};

const buildCFGVisData = (graph: CFGGraph | undefined): { nodes: VisNode[], edges: VisEdge[] } => {
  const visNodes: VisNode[] = [];
  const visEdges: VisEdge[] = [];
  if (!graph || !graph.nodes) return { nodes: visNodes, edges: visEdges };
  graph.nodes.forEach((n: CFGNode) => {
    let content = (n.content || '').replace(/["\\]/g, "'").replace(/[\[\]{}()<>]/g, ' ');
    if (content.length > 40) content = content.substring(0, 40) + '...';
    visNodes.push({ id: n.id, label: `[L${n.line}]\n${content}`, group: n.type });
  });
  if (graph.edges && Array.isArray(graph.edges)) {
    graph.edges.forEach((e: CFGEdge) => visEdges.push({ from: e.source, to: e.target, label: e.type || '' }));
  }
  return { nodes: visNodes, edges: visEdges };
};

const metricsChartOption = computed(() => {
  if (activeAlgorithm.value?.evidence_type !== 'metric_comparison') return undefined;
  const ev = activeAlgorithm.value.evidence;
  if (!ev.metrics_a || !ev.metrics_b) return undefined;
  const m_a = ev.metrics_a;
  const m_b = ev.metrics_b;
  const keys = Object.keys(m_a);
  if (keys.length === 0) return undefined;
  const maxValues = keys.map(k => Math.max(m_a[k] || 0, m_b[k] || 0) * 1.2 || 10);

  return {
    backgroundColor: 'transparent',
    tooltip: { trigger: 'item' },
    legend: { data: ['Файл 1', 'Файл 2'], textStyle: { color: isDarkMode.value ? '#e0e0e0' : '#495057' }, bottom: 0 },
    radar: {
      indicator: keys.map((k, i) => ({ name: formatMetricKey(k), max: maxValues[i] })),
      splitArea: { areaStyle: { color: isDarkMode.value ? ['rgba(255,255,255,0.02)', 'rgba(255,255,255,0.05)'] : ['rgba(0,0,0,0.02)', 'rgba(0,0,0,0.05)'] } },
      splitLine: { lineStyle: { color: isDarkMode.value ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.1)' } },
      axisName: { color: isDarkMode.value ? '#e0e0e0' : '#212529' },
      axisLine: { lineStyle: { color: isDarkMode.value ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.1)' } }
    },
    series: [{
      name: 'Метрики',
      type: 'radar',
      data: [
        { value: keys.map(k => m_a[k]), name: 'Файл 1', itemStyle: { color: '#3b82f6' }, areaStyle: { color: 'rgba(59, 130, 246, 0.3)' } },
        { value: keys.map(k => m_b[k]), name: 'Файл 2', itemStyle: { color: '#ef4444' }, areaStyle: { color: 'rgba(239, 68, 68, 0.3)' } }
      ]
    }]
  };
});

const hasOccurrence = (hash: MatchedHash, sub: 'a' | 'b') => {
  return hash.occurrences?.some(o => o.submission === sub);
};
</script>

<template>
  <div class="min-vh-100 dashboard-bg pb-5" v-if="report">
    <nav class="navbar navbar-light bg-white bg-opacity-75 border-bottom py-3 backdrop-blur sticky-top z-3 shadow-sm">
      <div class="container-fluid px-4">
        <div class="d-flex align-items-center">
          <button @click="router.push('/')" class="btn btn-outline-primary rounded-pill me-3 px-4 fw-bold d-flex align-items-center transition-all hover-glow">
            <i class="bi bi-arrow-left me-2"></i> Назад
          </button>
          <span class="navbar-brand fw-bold fs-4 mb-0 text-dark d-flex align-items-center">
            <i class="bi bi-layers-half text-primary me-2 fs-3 glow-text-primary"></i>
            Аналітика Коду
          </span>
        </div>
        <div class="d-flex align-items-center">
          <button @click="openExportModal" class="btn btn-outline-danger rounded-pill me-3 px-4 fw-bold d-flex align-items-center transition-all hover-glow shadow-sm bg-white">
            <i class="bi bi-file-earmark-pdf me-2"></i> Експорт в PDF
          </button>
          <button @click="toggleTheme" class="btn btn-light rounded-circle shadow-sm border hover-lift me-3 d-flex align-items-center justify-content-center" style="width: 40px; height: 40px;" title="Змінити тему">
            <i class="bi fs-5" :class="isDarkMode ? 'bi-sun-fill text-warning' : 'bi-moon-stars-fill text-secondary'"></i>
          </button>
          <div class="badge bg-light border px-4 py-2 rounded-pill text-secondary shadow-sm font-monospace">
            ID: {{ report.analysis_id }}
          </div>
        </div>
      </div>
    </nav>

    <div class="container-fluid px-4 mt-4">
      <div class="row g-4 h-100">
        <div class="col-xl-3 col-lg-4 d-flex flex-column gap-4">

           <div v-if="isMultiReport" class="glass-card p-4 position-relative overflow-hidden shadow-sm">
              <h6 class="text-muted text-uppercase fw-bold mb-3" style="letter-spacing: 1px; font-size: 0.75rem;">
                <i class="bi bi-files me-2"></i>Файли для порівняння
              </h6>
              <select v-model="selectedResultIndex" class="form-select border-0 bg-light rounded-3 shadow-sm fw-medium text-dark py-2 px-3" style="cursor: pointer;">
                <option v-for="(res, idx) in multiResults" :key="res.document_id || idx" :value="idx">
                  {{ res.target_file?.filename || 'Файл ' + (idx + 1) }} ({{ Math.round((res.global_similarity_score || 0) * 100) }}%)
                </option>
              </select>
           </div>

           <div class="glass-card p-5 text-center position-relative overflow-hidden">
             <div class="position-absolute top-0 start-0 w-100 h-100 opacity-25" :style="`background: radial-gradient(circle at center, ${getScoreColorHex(globalScore)} 0%, transparent 70%);`"></div>
             <h5 class="fw-bold text-dark mb-4 position-relative z-1 text-uppercase tracking-wider fs-6">Глобальна схожість</h5>

             <div class="position-relative d-inline-flex align-items-center justify-content-center mb-4" style="width: 200px; height: 200px;">
               <div class="position-absolute w-100 h-100 rounded-circle" :style="`background: ${getScoreColorHex(globalScore)}; filter: blur(40px); opacity: 0.15;`"></div>
               <svg class="w-100 h-100 position-relative z-1" viewBox="0 0 100 100">
                 <circle cx="50" cy="50" r="42" fill="none" stroke="rgba(0,0,0,0.05)" stroke-width="8" />
                 <circle cx="50" cy="50" r="42" fill="none"
                         :stroke="getScoreColorHex(globalScore)"
                         stroke-width="8"
                         stroke-dasharray="264"
                         :stroke-dashoffset="264 - (264 * globalScore) / 100"
                         stroke-linecap="round"
                         transform="rotate(-90 50 50)"
                         style="transition: stroke-dashoffset 2s cubic-bezier(0.4, 0, 0.2, 1);" />
               </svg>
               <div class="position-absolute text-dark z-2 d-flex flex-column align-items-center justify-content-center w-100">
                 <span class="fw-black glow-text" style="font-size: 3.5rem; line-height: 1; letter-spacing: -2px;" :style="`color: ${getScoreColorHex(globalScore)}`">{{ globalScore }}%</span>
               </div>
             </div>

             <div class="position-relative z-1">
                <span class="badge border bg-white px-4 py-2 rounded-pill fs-6 fw-bold shadow-sm" :style="`color: ${getScoreColorHex(globalScore)}; border-color: ${getScoreColorHex(globalScore)} !important;`">
                  <i class="bi me-2" :class="globalScore < 30 ? 'bi-shield-check' : globalScore < 70 ? 'bi-exclamation-triangle' : 'bi-x-octagon'"></i>
                  {{ globalScore < 30 ? 'Низька схожість' : globalScore < 70 ? 'Середня схожість' : 'Високий ризик' }}
                </span>
             </div>
           </div>

           <div class="glass-card p-3 flex-grow-1 custom-scrollbar overflow-auto" style="min-height: 400px; height: 0;">
              <h6 class="text-muted text-uppercase fw-bold mb-3 px-3" style="letter-spacing: 1.5px; font-size: 0.75rem;">Дерево Аналізу</h6>

              <div v-if="report.error" class="alert alert-danger m-2 py-2 px-3 small border-0 bg-danger bg-opacity-10 text-danger rounded-3 fw-medium">
                 <i class="bi bi-exclamation-triangle-fill me-2"></i> {{ report.error }}
              </div>

              <div class="d-flex flex-column gap-2" v-else>
                <div v-for="cat in report.categories_results" :key="cat.category_name">
                   <button class="btn w-100 text-start d-flex justify-content-between align-items-center p-3 rounded-4 glass-btn"
                           :class="{'glass-btn-active': activeCategory === cat.category_name}"
                           @click="selectCategory(cat)">
                       <span class="fw-bold text-dark d-flex align-items-center">
                         <i class="bi me-3 fs-5 opacity-75 text-primary" :class="{
                           'bi-file-text': cat.category_name === 'text_based',
                           'bi-braces': cat.category_name === 'token_based',
                           'bi-diagram-3': cat.category_name === 'tree_based',
                           'bi-share': cat.category_name === 'graph_based',
                           'bi-bar-chart': cat.category_name === 'metrics_based'
                         }"></i>
                         {{ formatCategoryName(cat.category_name) }}
                       </span>
                       <span class="badge rounded-pill shadow-sm" :class="getBgColor(cat.category_similarity_score * 100)">{{ formatScore(cat.category_similarity_score) }}</span>
                   </button>

                   <transition name="slide-fade">
                     <div v-if="activeCategory === cat.category_name" class="ps-4 pe-2 py-2 d-flex flex-column gap-1 overflow-hidden border-start border-primary border-opacity-25 ms-4 mt-2 mb-2">
                         <button v-for="algo in cat.algorithms" :key="algo.method"
                                 class="btn w-100 text-start p-2 rounded-3 small algo-btn"
                                 :class="{'algo-btn-active': activeAlgorithm?.method === algo.method}"
                                 @click="selectAlgorithm(cat, algo)">
                             <div class="d-flex justify-content-between align-items-center">
                                 <span class="text-dark fw-medium"><i class="bi bi-cpu me-2 text-primary opacity-75"></i> {{ formatMethodName(algo.method) }}</span>
                                 <span class="text-muted fw-bold font-monospace" style="font-size: 0.75rem;">{{ formatScore(algo.similarity_score) }}</span>
                             </div>
                         </button>
                     </div>
                   </transition>
                </div>
              </div>
           </div>
        </div>

        <div class="col-xl-9 col-lg-8">
           <div class="glass-card h-100 d-flex flex-column overflow-hidden">

              <div v-if="!activeAlgorithm" class="d-flex flex-column align-items-center justify-content-center h-100 p-5 text-muted">
                <i class="bi bi-mouse-3 display-1 mb-3 opacity-50"></i>
                <h4 class="fw-light text-uppercase tracking-wider">Оберіть алгоритм для деталей</h4>
              </div>

              <div v-else class="d-flex flex-column h-100 animate__fadeIn">
                <div class="p-4 border-bottom d-flex justify-content-between align-items-center bg-light">
                   <div>
                     <h3 class="fw-bolder text-dark mb-1 d-flex align-items-center">
                       <i class="bi bi-box-seam text-primary me-3"></i> {{ formatMethodName(activeAlgorithm.method) }}
                     </h3>
                     <span class="text-muted small font-monospace"><i class="bi bi-info-circle me-1"></i> Тип доказів: {{ formatEvidenceType(activeAlgorithm.evidence_type) }}</span>
                   </div>
                   <div class="text-end">
                     <div class="text-muted small mb-2 fw-bold text-uppercase tracking-wider" style="font-size: 0.7rem;">Рівень Збігу</div>
                     <span class="badge px-4 py-2 rounded-pill fs-5 shadow-sm border" :class="getBgColor(activeAlgorithm.similarity_score * 100)">
                       {{ formatScore(activeAlgorithm.similarity_score) }}
                     </span>
                   </div>
                </div>

                <!-- Body -->
                <div class="p-4 flex-grow-1 overflow-auto custom-scrollbar bg-white">

                   <!-- MAC OS DUAL CODE VIEWER (Для структурних збігів) -->
                   <div v-if="['text_highlight', 'ast_tree_mapping', 'graph_mapping'].includes(activeAlgorithm.evidence_type) && report.source_files" class="row g-4 mb-5">
                     <div class="col-xl-6">
                       <div class="mac-window rounded-4 overflow-hidden border shadow-sm d-flex flex-column h-100">
                          <div class="mac-header bg-light d-flex align-items-center px-3 py-2 border-bottom">
                             <div class="d-flex gap-2">
                                <div class="rounded-circle bg-danger" style="width: 12px; height: 12px;"></div>
                                <div class="rounded-circle bg-warning" style="width: 12px; height: 12px;"></div>
                                <div class="rounded-circle bg-success" style="width: 12px; height: 12px;"></div>
                             </div>
                             <span class="ms-3 font-monospace text-secondary small fw-bold">{{ report.source_files.name_a || 'Файл 1' }}</span>
                          </div>
                          <div class="mac-body bg-white flex-grow-1 position-relative" style="height: 500px;">
                             <VueMonacoEditor
                               v-model:value="report.source_files.file_a"
                               :theme="isDarkMode ? 'vs-dark' : 'vs'"
                               :language="report.language || 'cpp'"
                               :options="{ readOnly: true, minimap: { enabled: false }, scrollBeyondLastLine: false, fontSize: 13, smoothScrolling: true }"
                               @mount="(editor, monaco) => handleEditorMount(editor, monaco, 'a')"
                             />
                          </div>
                       </div>
                     </div>
                     <div class="col-xl-6">
                       <div class="mac-window rounded-4 overflow-hidden border shadow-sm d-flex flex-column h-100">
                          <div class="mac-header bg-light d-flex align-items-center px-3 py-2 border-bottom">
                             <div class="d-flex gap-2">
                                <div class="rounded-circle bg-danger" style="width: 12px; height: 12px;"></div>
                                <div class="rounded-circle bg-warning" style="width: 12px; height: 12px;"></div>
                                <div class="rounded-circle bg-success" style="width: 12px; height: 12px;"></div>
                             </div>
                             <span class="ms-3 font-monospace text-secondary small fw-bold">{{ report.source_files.name_b || 'Файл 2' }}</span>
                          </div>
                          <div class="mac-body bg-white flex-grow-1 position-relative" style="height: 500px;">
                             <VueMonacoEditor
                               v-model:value="report.source_files.file_b"
                               :theme="isDarkMode ? 'vs-dark' : 'vs'"
                               :language="report.language || 'cpp'"
                               :options="{ readOnly: true, minimap: { enabled: false }, scrollBeyondLastLine: false, fontSize: 13, smoothScrolling: true }"
                               @mount="(editor, monaco) => handleEditorMount(editor, monaco, 'b')"
                             />
                          </div>
                       </div>
                     </div>
                   </div>

                   <div v-if="activeAlgorithm.evidence_type === 'text_highlight' && activeAlgorithm.evidence?.matched_blocks?.length">
                      <h6 class="text-muted text-uppercase fw-bold mb-3 tracking-wider"><i class="bi bi-layout-text-sidebar-reverse me-2"></i> Деталі текстових збігів</h6>
                      <div class="accordion light-accordion shadow-sm" :id="'acc-' + activeAlgorithm.method">
                        <div class="accordion-item" v-for="(block, bIdx) in activeAlgorithm.evidence.matched_blocks" :key="bIdx">
                          <h2 class="accordion-header">
                            <button class="accordion-button fw-bold" :class="{'collapsed': bIdx !== 0}" type="button" data-bs-toggle="collapse" :data-bs-target="'#col-' + activeAlgorithm.method + '-' + bIdx">
                              <i class="bi bi-file-diff text-primary me-3 fs-5"></i>
                              Збіг #{{ bIdx + 1 }} <span class="badge bg-light text-secondary ms-3 font-monospace fw-normal border">Рядки: {{ block.start_line_a }}-{{ block.end_line_a }} ⟷ {{ block.start_line_b }}-{{ block.end_line_b }}</span>
                            </button>
                          </h2>
                          <div :id="'col-' + activeAlgorithm.method + '-' + bIdx" class="accordion-collapse collapse" :class="{'show': bIdx === 0}" :data-bs-parent="'#acc-' + activeAlgorithm.method">
                            <div class="accordion-body p-0 d-flex flex-column flex-md-row">
                              <div class="w-100 w-md-50 border-end border-opacity-10">
                                <div class="bg-light px-4 py-2 border-bottom small fw-bold text-muted d-flex align-items-center">
                                  <i class="bi bi-file-earmark-code me-2"></i> {{ block.file_a || 'Файл 1' }}
                                </div>
                                <pre class="m-0 p-4 text-danger custom-scrollbar bg-white" style="white-space: pre-wrap; font-family: 'Fira Code', monospace; font-size: 0.85rem;"><code>{{ block.content_a }}</code></pre>
                              </div>
                              <div class="w-100 w-md-50">
                                <div class="bg-light px-4 py-2 border-bottom small fw-bold text-muted d-flex align-items-center">
                                  <i class="bi bi-file-earmark-code me-2"></i> {{ block.file_b || 'Файл 2' }}
                                </div>
                                <pre class="m-0 p-4 text-danger custom-scrollbar bg-white" style="white-space: pre-wrap; font-family: 'Fira Code', monospace; font-size: 0.85rem;"><code>{{ block.content_b }}</code></pre>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                   </div>

                   <div v-else-if="activeAlgorithm.evidence_type === 'token_sequence'">
                      <div v-if="!activeAlgorithm.evidence?.matched_hashes || activeAlgorithm.evidence.matched_hashes.length === 0" class="text-center py-5">
                        <i class="bi bi-shield-check text-success display-1 d-block mb-3 opacity-50 glow-text"></i>
                        <h5 class="text-muted">Збігів токенів не знайдено</h5>
                      </div>
                      <div v-else class="table-responsive bg-white rounded-4 border shadow-sm">
                        <table class="table light-table align-middle mb-0">
                          <thead class="bg-light text-uppercase tracking-wider" style="font-size: 0.75rem;">
                            <tr>
                              <th class="py-4 px-4">Опис збігу (Хеш послідовності)</th>
                              <th class="py-4 px-4 text-center">Наявність (Файл 1)</th>
                              <th class="py-4 px-4 text-center">Наявність (Файл 2)</th>
                            </tr>
                          </thead>
                          <tbody>
                            <tr v-for="(hash, hIdx) in activeAlgorithm.evidence.matched_hashes" :key="hIdx">
                              <td class="py-3 px-4">
                                <span class="badge bg-light text-secondary border me-3 font-monospace px-2 py-1">{{ hash.hash_value }}</span>
                                <span class="fw-medium text-dark font-monospace small">{{ formatTokenSequence(hash.token_sequence) }}</span>
                              </td>
                              <td class="py-3 px-4 text-center">
                                 <span v-if="hasOccurrence(hash, 'a')" class="badge bg-primary bg-opacity-25 text-primary border border-primary border-opacity-50 px-3 py-2 rounded-pill">
                                    Знайдено
                                 </span>
                                 <span v-else class="text-muted">-</span>
                              </td>
                              <td class="py-3 px-4 text-center">
                                 <span v-if="hasOccurrence(hash, 'b')" class="badge bg-danger bg-opacity-25 text-danger border border-danger border-opacity-50 px-3 py-2 rounded-pill">
                                    Знайдено
                                 </span>
                                 <span v-else class="text-muted">-</span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </div>
                   </div>

                   <div v-else-if="activeAlgorithm.evidence_type === 'ast_tree_mapping'">
                      <div class="d-flex align-items-center justify-content-between mb-4">
                        <h6 class="text-muted text-uppercase fw-bold mb-0 tracking-wider"><i class="bi bi-diagram-3 me-2"></i> Зведення збігів AST</h6>
                        <span class="badge bg-primary bg-opacity-25 border border-primary border-opacity-50 rounded-pill px-3 py-2 text-primary shadow-sm">{{ activeAlgorithm.evidence.length || activeAlgorithm.evidence.matched_subtrees?.length || 0 }} збігів загалом</span>
                      </div>

                      <div v-if="Array.isArray(activeAlgorithm.evidence)">
                          <div class="table-responsive mb-5 bg-white rounded-4 border shadow-sm">
                            <table class="table light-table mb-0 align-middle">
                              <thead class="bg-light text-uppercase tracking-wider" style="font-size: 0.75rem;">
                                <tr>
                                  <th class="py-4 px-4">Тип збігу</th>
                                  <th class="py-4 px-4 text-center">Кількість</th>
                                  <th class="py-4 px-4">Рівень</th>
                                </tr>
                              </thead>
                              <tbody>
                                <tr v-for="(match, idx) in aggregateMatches(activeAlgorithm.evidence)" :key="idx">
                                  <td class="py-3 px-4 fw-medium text-dark"><i class="bi bi-diagram-2 text-secondary opacity-75 me-3 fs-5"></i>{{ formatMatchType(match.type) }}</td>
                                  <td class="py-3 px-4 text-center"><span class="badge bg-light border rounded-pill px-3 py-2 fs-6 text-dark">{{ match.count }}</span></td>
                                  <td class="py-3 px-4">
                                     <span class="badge rounded-pill px-3 py-2 border" :class="match.severity === 'high' ? 'bg-danger bg-opacity-25 text-danger border-danger border-opacity-50' : match.severity === 'med' ? 'bg-warning bg-opacity-25 text-warning border-warning border-opacity-50' : 'bg-info bg-opacity-25 text-info border-info border-opacity-50'">{{ formatSeverity(match.severity) }}</span>
                                  </td>
                                </tr>
                              </tbody>
                            </table>
                         </div>

                          <div class="accordion light-accordion mt-4 shadow-sm" :id="'acc-ast-' + activeAlgorithm.method">
                            <div class="accordion-item border-0">
                              <h2 class="accordion-header">
                                <button class="accordion-button collapsed fw-bold py-3" type="button" data-bs-toggle="collapse" :data-bs-target="'#col-ast-' + activeAlgorithm.method">
                                  <i class="bi bi-braces-asterisk text-primary fs-5 me-3"></i> Детальний мапінг рядків
                                </button>
                              </h2>
                              <div :id="'col-ast-' + activeAlgorithm.method" class="accordion-collapse collapse" :data-bs-parent="'#acc-ast-' + activeAlgorithm.method">
                                <div class="accordion-body p-0 custom-scrollbar" style="max-height: 450px; overflow-y: auto;">
                                   <ul class="list-group list-group-flush">
                                      <li v-for="(m, idx) in activeAlgorithm.evidence" :key="idx" class="list-group-item bg-transparent flex-column py-4 gap-3 border-bottom border-opacity-10">
                                         <div class="d-flex justify-content-between align-items-center w-100 mb-2">
                                           <span class="small fw-bold text-dark"><i class="bi bi-link-45deg text-secondary fs-5 me-2"></i>{{ formatMatchType(m.type) }} (Рівень: {{ formatSeverity(m.severity) }})</span>
                                           <span v-if="!((m.leftLines && m.leftLines.length) || (m.rightLines && m.rightLines.length))" class="badge bg-light text-secondary border px-3 py-2 rounded-pill">Структурний збіг (весь код)</span>
                                         </div>
                                         <div class="d-flex flex-column flex-md-row gap-3 w-100" v-if="(m.leftLines && m.leftLines.length) || (m.rightLines && m.rightLines.length)">
                                            <div class="flex-grow-1 border border-opacity-10 rounded-3 bg-white overflow-hidden" style="flex-basis: 0;">
                                               <div class="bg-light px-3 py-2 border-bottom text-muted small fw-bold d-flex justify-content-between">
                                                 <span>Файл 1</span>
                                               </div>
                                               <pre class="m-0 p-3 text-primary custom-scrollbar font-monospace" style="font-size: 0.8rem; max-height: 150px; overflow-y: auto; white-space: pre-wrap; word-wrap: break-word;"><code>{{ getLinesContent(report.source_files?.file_a, m.leftLines, 'ast_tree_mapping') }}</code></pre>
                                            </div>
                                            <div class="d-flex align-items-center justify-content-center">
                                              <div class="bg-light border rounded-circle p-2 shadow-sm d-flex align-items-center justify-content-center">
                                                <i class="bi bi-arrow-left-right text-muted"></i>
                                              </div>
                                            </div>
                                            <div class="flex-grow-1 border border-opacity-10 rounded-3 bg-white overflow-hidden" style="flex-basis: 0;">
                                               <div class="bg-light px-3 py-2 border-bottom text-muted small fw-bold d-flex justify-content-between">
                                                 <span>Файл 2</span>
                                               </div>
                                               <pre class="m-0 p-3 text-danger custom-scrollbar font-monospace" style="font-size: 0.8rem; max-height: 150px; overflow-y: auto; white-space: pre-wrap; word-wrap: break-word;"><code>{{ getLinesContent(report.source_files?.file_b, m.rightLines, 'ast_tree_mapping') }}</code></pre>
                                            </div>
                                         </div>
                                      </li>
                                   </ul>
                                </div>
                              </div>
                            </div>
                          </div>
                      </div>
                      <div v-else-if="activeAlgorithm.evidence?.matched_subtrees" class="row g-4 mt-2">
                          <div class="col-12" v-for="(subtree, sIdx) in activeAlgorithm.evidence.matched_subtrees" :key="sIdx">
                            <div class="p-4 border rounded-4 bg-white shadow-sm">
                              <div class="fw-bold mb-4 fs-5 text-dark border-bottom pb-3">
                                <i class="bi bi-diagram-3 text-primary me-2"></i> Вузол: <span class="text-primary">{{ subtree.node_type }}</span>
                              </div>
                              <div class="row g-4">
                                <div class="col-md-6">
                                  <div class="card h-100 border-0 bg-transparent shadow-none">
                                    <div class="card-header bg-light text-dark fw-bold py-3 border rounded-top-3"><i class="bi bi-diagram-2 me-2"></i> AST Вузли (Файл 1)</div>
                                    <div class="p-3 border-bottom border-start border-end bg-white d-flex justify-content-center">
                                      <InteractiveGraph :graph-data="buildASTVisData(subtree.nodes_a)" />
                                    </div>
                                    <div class="card-body p-0 custom-scrollbar bg-white border border-top-0 rounded-bottom-3" style="max-height: 200px; overflow-y: auto;">
                                      <ul class="list-group list-group-flush small">
                                         <li class="list-group-item bg-transparent text-dark border-bottom py-2" v-for="node in subtree.nodes_a" :key="node.id">
                                            <i class="bi bi-record-circle text-primary me-3" style="font-size: 0.7rem;"></i>
                                            <span class="fw-medium text-dark">{{ node.label }}</span>
                                            <span class="text-muted ms-2 font-monospace" style="font-size: 0.7rem;">[{{ node.id }}]</span>
                                         </li>
                                      </ul>
                                    </div>
                                  </div>
                                </div>
                                <div class="col-md-6">
                                  <div class="card h-100 border-0 bg-transparent shadow-none">
                                    <div class="card-header bg-light text-dark fw-bold py-3 border rounded-top-3"><i class="bi bi-diagram-2-fill me-2 text-danger"></i> AST Вузли (Файл 2)</div>
                                    <div class="p-3 border-bottom border-start border-end bg-white d-flex justify-content-center">
                                      <InteractiveGraph :graph-data="buildASTVisData(subtree.nodes_b)" />
                                    </div>
                                    <div class="card-body p-0 custom-scrollbar bg-white border border-top-0 rounded-bottom-3" style="max-height: 200px; overflow-y: auto;">
                                      <ul class="list-group list-group-flush small">
                                         <li class="list-group-item bg-transparent text-dark border-bottom py-2" v-for="node in subtree.nodes_b" :key="node.id">
                                            <i class="bi bi-record-circle text-danger me-3" style="font-size: 0.7rem;"></i>
                                            <span class="fw-medium text-dark">{{ node.label }}</span>
                                            <span class="text-muted ms-2 font-monospace" style="font-size: 0.7rem;">[{{ node.id }}]</span>
                                         </li>
                                      </ul>
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </div>
                          </div>
                      </div>
                   </div>

                   <div v-else-if="activeAlgorithm.evidence_type === 'graph_mapping'">
                      <div class="d-flex align-items-center justify-content-between mb-4">
                        <h6 class="text-muted text-uppercase fw-bold mb-0 tracking-wider"><i class="bi bi-share me-2"></i> Зведення збігів графів</h6>
                        <span class="badge bg-primary bg-opacity-25 border border-primary border-opacity-50 rounded-pill px-3 py-2 text-primary shadow-sm">{{ activeAlgorithm.evidence.matches?.length || 0 }} збігів</span>
                      </div>
                      <div class="table-responsive mb-5 bg-white rounded-4 border shadow-sm">
                        <table class="table light-table mb-0 align-middle">
                          <thead class="bg-light text-uppercase tracking-wider" style="font-size: 0.75rem;">
                            <tr>
                              <th class="py-4 px-4">Тип збігу</th>
                              <th class="py-4 px-4 text-center">Кількість</th>
                              <th class="py-4 px-4">Рівень</th>
                            </tr>
                          </thead>
                          <tbody>
                            <tr v-for="(match, idx) in aggregateMatches(activeAlgorithm.evidence.matches)" :key="idx">
                              <td class="py-3 px-4 fw-medium text-dark"><i class="bi bi-bezier2 text-secondary opacity-75 me-3 fs-5"></i>{{ formatMatchType(match.type) }}</td>
                              <td class="py-3 px-4 text-center"><span class="badge bg-light border rounded-pill px-3 py-2 fs-6 text-dark">{{ match.count }}</span></td>
                              <td class="py-3 px-4">
                                 <span class="badge rounded-pill px-3 py-2 border" :class="match.severity === 'high' ? 'bg-danger bg-opacity-25 text-danger border-danger border-opacity-50' : match.severity === 'med' ? 'bg-warning bg-opacity-25 text-warning border-warning border-opacity-50' : 'bg-info bg-opacity-25 text-info border-info border-opacity-50'">{{ formatSeverity(match.severity) }}</span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </div>

                      <div class="row g-4">
                        <div class="col-md-6">
                           <div class="card h-100 shadow-sm border rounded-4 overflow-hidden">
                             <div class="card-header bg-light border-bottom p-3 d-flex align-items-center text-dark fw-bold">
                               <i class="bi bi-box me-2 text-primary"></i> Вузли графа 1
                             </div>
                             <div class="p-3 border-bottom bg-white d-flex justify-content-center">
                                <InteractiveGraph :graph-data="buildCFGVisData(activeAlgorithm.evidence.graph_a)" />
                             </div>
                             <div class="card-body p-0 custom-scrollbar bg-white" style="max-height: 350px; overflow-y: auto;">
                               <ul class="list-group list-group-flush">
                                 <li class="list-group-item bg-transparent d-flex align-items-start py-3 border-bottom" v-for="node in activeAlgorithm.evidence.graph_a?.nodes" :key="node.id">
                                   <div class="flex-grow-1">
                                     <div class="mb-2"><span class="badge bg-primary bg-opacity-25 text-primary border border-primary border-opacity-50" style="font-size: 0.7rem;">{{ formatNodeType(node.type) }}</span></div>
                                     <code class="text-dark bg-light border px-3 py-2 rounded-3 d-block font-monospace" style="font-size: 0.8rem; word-break: break-all;">{{ node.content }}</code>
                                   </div>
                                 </li>
                               </ul>
                             </div>
                           </div>
                        </div>
                        <div class="col-md-6">
                           <div class="card h-100 shadow-sm border rounded-4 overflow-hidden">
                             <div class="card-header bg-light border-bottom p-3 d-flex align-items-center text-dark fw-bold">
                               <i class="bi bi-box me-2 text-danger"></i> Вузли графа 2
                             </div>
                             <div class="p-3 border-bottom bg-white d-flex justify-content-center">
                                <InteractiveGraph :graph-data="buildCFGVisData(activeAlgorithm.evidence.graph_b)" />
                             </div>
                             <div class="card-body p-0 custom-scrollbar bg-white" style="max-height: 350px; overflow-y: auto;">
                               <ul class="list-group list-group-flush">
                                 <li class="list-group-item bg-transparent d-flex align-items-start py-3 border-bottom" v-for="node in activeAlgorithm.evidence.graph_b?.nodes" :key="node.id">
                                   <div class="flex-grow-1">
                                     <div class="mb-2"><span class="badge bg-danger bg-opacity-25 text-danger border border-danger border-opacity-50" style="font-size: 0.7rem;">{{ formatNodeType(node.type) }}</span></div>
                                     <code class="text-dark bg-light border px-3 py-2 rounded-3 d-block font-monospace" style="font-size: 0.8rem; word-break: break-all;">{{ node.content }}</code>
                                   </div>
                                 </li>
                               </ul>
                             </div>
                           </div>
                        </div>
                      </div>
                   </div>

                   <div v-else-if="activeAlgorithm.evidence_type === 'metric_comparison'">
                      <div class="row g-4">
                        <div class="col-xl-4 col-lg-5">
                          <div class="d-flex flex-column gap-4 h-100">
                            <div class="glass-card border p-4 position-relative overflow-hidden flex-grow-1 shadow-sm">
                            <h5 class="text-primary mb-4 d-flex align-items-center fw-bold position-relative z-1"><i class="bi bi-bar-chart-steps me-3"></i> Метрики Файл 1</h5>
                            <div class="d-flex flex-column gap-2 position-relative z-1">
                               <div v-for="(val, key) in activeAlgorithm.evidence.metrics_a" :key="key" class="d-flex justify-content-between align-items-center border-bottom py-3">
                                  <span class="text-muted fw-medium">{{ formatMetricKey(String(key)) }}</span>
                                  <span class="fw-bold fs-4 text-dark font-monospace">{{ typeof val === 'number' && !Number.isInteger(val) ? val.toFixed(2) : val }}</span>
                               </div>
                            </div>
                          </div>
                            <div class="glass-card border p-4 position-relative overflow-hidden flex-grow-1 shadow-sm">
                            <h5 class="text-danger mb-4 d-flex align-items-center fw-bold position-relative z-1"><i class="bi bi-bar-chart-steps me-3"></i> Метрики Файл 2</h5>
                            <div class="d-flex flex-column gap-2 position-relative z-1">
                               <div v-for="(val, key) in activeAlgorithm.evidence.metrics_b" :key="key" class="d-flex justify-content-between align-items-center border-bottom py-3">
                                  <span class="text-muted fw-medium">{{ formatMetricKey(String(key)) }}</span>
                                  <span class="fw-bold fs-4 text-dark font-monospace">{{ typeof val === 'number' && !Number.isInteger(val) ? val.toFixed(2) : val }}</span>
                               </div>
                            </div>
                          </div>
                          </div>
                        </div>
                        <div class="col-xl-8 col-lg-7">
                           <div class="glass-card p-4 h-100 d-flex flex-column align-items-center justify-content-center border shadow-sm">
                              <h5 class="text-muted mb-4 fw-bold text-uppercase tracking-wider">Радар Метрик</h5>
                              <v-chart :option="metricsChartOption" class="w-100" style="min-height: 400px;" autoresize />
                           </div>
                        </div>
                      </div>
                      <div v-if="activeAlgorithm.evidence.conclusion" class="alert bg-info bg-opacity-10 border border-info mt-5 mb-0 text-center shadow-sm d-flex justify-content-center align-items-center rounded-4 py-4 backdrop-blur">
                        <i class="bi bi-robot me-3 display-5 text-info glow-text"></i>
                        <span class="text-dark fs-5 fw-medium">{{ activeAlgorithm.evidence.conclusion }}</span>
                      </div>
                   </div>

                   <div v-else>
                      <div class="bg-light p-4 rounded-4 shadow-sm border custom-scrollbar" style="max-height: 400px; overflow: auto;">
                        <pre class="m-0 text-muted small" style="font-family: 'Fira Code', monospace;"><code>{{ JSON.stringify(activeAlgorithm.evidence, null, 2) }}</code></pre>
                      </div>
                   </div>

                </div>
             </div>
           </div>
        </div>

      </div>
    </div>

    <!-- Export Modal -->
    <div v-if="showExportModal" class="modal d-block animate__animated animate__fadeIn animate__faster" tabindex="-1" style="background: rgba(0,0,0,0.5); z-index: 1050; backdrop-filter: blur(5px);">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content rounded-4 shadow-lg border-0">
          <div class="modal-header border-bottom px-4 py-3 bg-light rounded-top-4">
            <h5 class="modal-title fw-bold text-dark"><i class="bi bi-file-earmark-pdf text-danger me-2"></i> Налаштування Експорту</h5>
            <button type="button" class="btn-close" @click="showExportModal = false"></button>
          </div>
          <div class="modal-body p-4 custom-scrollbar" style="max-height: 60vh; overflow-y: auto;">
            <p class="text-muted small mb-4">Оберіть алгоритми, результати яких будуть розгорнуті та включені до PDF-звіту.</p>
            <div v-for="cat in report?.categories_results" :key="cat.category_name" class="mb-4">
              <h6 class="fw-bold text-dark mb-2 border-bottom pb-2"><i class="bi bi-diagram-3 text-primary me-2"></i>{{ formatCategoryName(cat.category_name) }}</h6>
              <div class="ms-3 mt-2 d-flex flex-column gap-2">
                <div class="form-check" v-for="algo in cat.algorithms" :key="algo.method">
                  <input class="form-check-input cursor-pointer shadow-sm" type="checkbox" :id="'export-' + algo.method" v-model="exportSelections[algo.method]">
                  <label class="form-check-label cursor-pointer small fw-medium text-secondary" :for="'export-' + algo.method">
                    {{ formatMethodName(algo.method) }}
                  </label>
                </div>
              </div>
            </div>
          </div>
          <div class="modal-footer border-top px-4 py-3 bg-light rounded-bottom-4">
            <button type="button" class="btn btn-light border rounded-pill px-4" @click="showExportModal = false" :disabled="isGeneratingPdf">Скасувати</button>
            <button type="button" class="btn btn-danger rounded-pill px-4 fw-bold shadow-sm" @click="confirmExport" :disabled="isGeneratingPdf">
              <span v-if="isGeneratingPdf" class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
              <i v-else class="bi bi-printer me-2"></i> {{ isGeneratingPdf ? 'Створення...' : 'Сформувати звіт' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="isGeneratingPdf" style="position: absolute; top: 0; left: 0; width: 1000px; opacity: 0; pointer-events: none; z-index: -1000; overflow: hidden;">
      <ReportExportView :hidden-render="true" :result-index="isMultiReport ? selectedResultIndex : 0" :methods="exportSelectedMethods" @pdf-generated="onPdfGenerated" />
    </div>
  </div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Fira+Code:wght@400;500;700&family=Inter:wght@400;600;800;900&display=swap');

.dashboard-bg {
  background: #f8f9fa;
  background-image:
    radial-gradient(at 0% 0%, rgba(13, 110, 253, 0.1) 0px, transparent 50%),
    radial-gradient(at 100% 0%, rgba(220, 53, 69, 0.05) 0px, transparent 50%);
  color: #212529;
  font-family: 'Inter', system-ui, sans-serif;
}

.backdrop-blur {
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
}

.glass-card {
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  border: 1px solid rgba(0, 0, 0, 0.08);
  border-radius: 1.5rem;
  box-shadow: 0 10px 30px -5px rgba(0, 0, 0, 0.05);
}

.glass-btn {
  background: transparent;
  border: 1px solid transparent;
  transition: all 0.3s ease;
  color: #6c757d;
}
.glass-btn:hover {
  background: rgba(0, 0, 0, 0.03);
}
.glass-btn-active {
  background: rgba(13, 110, 253, 0.08) !important;
  border: 1px solid rgba(13, 110, 253, 0.2) !important;
  box-shadow: inset 0 0 10px rgba(13, 110, 253, 0.05);
}

.algo-btn {
  background: transparent;
  border: none;
  transition: all 0.2s ease;
  color: #495057;
}
.algo-btn:hover {
  background: rgba(0, 0, 0, 0.04);
}
.algo-btn-active {
  background: rgba(0, 0, 0, 0.05) !important;
  border-left: 3px solid #0d6efd;
  border-radius: 0 0.5rem 0.5rem 0;
  font-weight: bold;
}

.fw-black {
  font-weight: 900;
}
.tracking-wider {
  letter-spacing: 0.05em;
}
.glow-text {
  text-shadow: 0 0 30px currentColor;
}
.glow-text-primary {
  text-shadow: 0 0 20px #0d6efd;
}
.hover-glow:hover {
  box-shadow: 0 0 15px rgba(13, 110, 253, 0.3);
}

:deep(.plagiarism-highlight) {
  background-color: rgba(220, 53, 69, 0.15) !important;
}
:deep(.plagiarism-margin) {
  background-color: rgba(220, 53, 69, 0.5) !important;
  width: 4px !important;
  margin-left: 4px;
}

.custom-scrollbar::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}
.custom-scrollbar::-webkit-scrollbar-track {
  background: rgba(0,0,0,0.05);
  border-radius: 4px;
}
.custom-scrollbar::-webkit-scrollbar-thumb {
  background: rgba(0,0,0,0.15);
  border-radius: 4px;
}
.custom-scrollbar::-webkit-scrollbar-thumb:hover {
  background: rgba(0,0,0,0.25);
}

.slide-fade-enter-active, .slide-fade-leave-active {
  transition: all 0.3s ease;
}
.slide-fade-enter-from, .slide-fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}

.animate__fadeIn {
  animation: fadeIn 0.4s ease-out forwards;
}
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.light-table {
  --bs-table-bg: transparent;
  --bs-table-color: #212529;
  color: #212529;
}
.light-table th { color: #6c757d; font-weight: 600; border-bottom: 1px solid rgba(0,0,0,0.1) !important; }
.light-table td { border-bottom: 1px solid rgba(0,0,0,0.05) !important; }
.light-table tbody tr:hover { background-color: rgba(0, 0, 0, 0.02) !important; }

.light-accordion .accordion-item {
  background-color: transparent;
  border: 1px solid rgba(0,0,0,0.08);
  margin-bottom: 0.5rem;
  border-radius: 1rem !important;
  overflow: hidden;
}
.light-accordion .accordion-button {
  background-color: rgba(0,0,0,0.02);
  color: #212529;
  box-shadow: none;
}
.light-accordion .accordion-button:not(.collapsed) {
  background-color: rgba(13, 110, 253, 0.05);
  color: #0d6efd;
}
.light-accordion .accordion-body {
  background-color: rgba(255,255,255,0.7);
  color: #495057;
}
</style>
