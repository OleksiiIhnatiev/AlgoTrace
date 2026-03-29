<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router';
import { analysisState } from '@/services/analysis.service';
import { computed, onMounted, ref, nextTick } from 'vue';
import InteractiveGraph from './InteractiveGraph.vue';
import type { Node as VisNode, Edge as VisEdge, Options as VisOptions } from 'vis-network';
import html2pdf from 'html2pdf.js';

interface MatchedBlock { start_line_a: number; end_line_a: number; start_line_b: number; end_line_b: number; content_a: string; content_b: string; file_a?: string; file_b?: string; }
interface MatchedHash { hash_value: string; token_sequence: string; occurrences: { submission: 'a' | 'b' }[]; }
interface ASTMatch { type: string; severity: string; leftLines: number[]; rightLines: number[]; }
interface ASTNode { id: string; label: string; children?: string[]; }
interface SubtreeMatch { node_type: string; nodes_a: ASTNode[]; nodes_b: ASTNode[]; }
interface GraphMatch { type: string; severity: string; left_lines: number[]; right_lines: number[]; }
interface CFGNode { id: string; line: number; content: string; type: string; }
interface CFGEdge { source: string; target: string; type?: string; }
interface CFGGraph { nodes: CFGNode[]; edges: CFGEdge[]; }
interface Evidence { matched_blocks?: MatchedBlock[]; matched_hashes?: MatchedHash[]; matched_subtrees?: SubtreeMatch[]; full_nodes_a?: ASTNode[]; full_nodes_b?: ASTNode[]; matches?: GraphMatch[]; graph_a?: CFGGraph; graph_b?: CFGGraph; metrics_a?: Record<string, number>; metrics_b?: Record<string, number>; conclusion?: string; length?: number; [index: number]: ASTMatch; }
interface Algorithm { method: string; similarity_score: number; evidence_type: string; evidence: Evidence; }
interface Category { category_name: string; category_similarity_score: number; algorithms: Algorithm[]; }
interface SourceFiles { name_a: string; file_a: string; name_b: string; file_b: string; }
interface Report { analysis_id: string; global_similarity_score: number; categories_results: Category[]; source_files: SourceFiles; language: string; }
interface RawReport {
  analysis_id: string;
  language?: string;
  global_similarity_score?: number;
  categories_results?: Category[];
  source_files?: SourceFiles;
  main_submission?: { filename: string; content: string };
  results?: Array<{ global_similarity_score: number; categories_results: Category[]; target_file?: { filename: string; content: string } }>;
}

const props = defineProps<{
  hiddenRender?: boolean;
  resultIndex?: number;
  methods?: string[];
}>();

const emit = defineEmits(['pdf-generated']);

const route = useRoute();
const router = useRouter();

const reportContainer = ref<HTMLElement | null>(null);
const isGenerating = ref(false);

onMounted(() => {
  if (!analysisState.currentReport) {
    if (!props.hiddenRender) router.push('/');
  } else if (props.hiddenRender) {
    nextTick(() => {
      downloadPdf();
    });
  }
});

const report = computed<Report | null>(() => {
  const raw = analysisState.currentReport as unknown as RawReport | null;
  if (!raw) return null;

  const isMulti = raw && 'results' in raw && Array.isArray(raw.results);
  const resultIdx = props.resultIndex !== undefined ? props.resultIndex : (route.query.resultIndex ? parseInt(route.query.resultIndex as string) : 0);

  let baseReport: Report;
  if (isMulti) {
    const result = raw.results![resultIdx];
    if (!result) return null;
    baseReport = {
      analysis_id: raw.analysis_id,
      global_similarity_score: result.global_similarity_score,
      categories_results: result.categories_results || [],
      source_files: {
        name_a: raw.main_submission?.filename || 'Файл 1',
        file_a: raw.main_submission?.content || '',
        name_b: result.target_file?.filename || 'Файл 2',
        file_b: result.target_file?.content || ''
      },
      language: raw.language || 'python'
    };
  } else {
    baseReport = raw as unknown as Report;
  }

  const selectedMethods = props.methods !== undefined ? props.methods : (route.query.methods ? (route.query.methods as string).split(',') : []);

  const filteredCategories = baseReport.categories_results.map(cat => {
    return {
      ...cat,
      algorithms: cat.algorithms.filter(algo => selectedMethods.includes(algo.method))
    };
  }).filter(cat => cat.algorithms.length > 0);

  return {
    ...baseReport,
    categories_results: filteredCategories
  };
});

const globalScore = computed(() => {
  return report.value?.global_similarity_score ? Math.round(report.value.global_similarity_score * 100) : 0;
});

const getScoreColorHex = (score: number) => {
  if (score < 30) return '#10b981';
  if (score < 70) return '#f59e0b';
  return '#ef4444';
};

const formatScore = (score: number) => Math.round((score || 0) * 100) + '%';

const formatCategoryName = (name: string) => {
  const map: Record<string, string> = { 'text_based': 'Текстовий аналіз', 'token_based': 'Токенний аналіз', 'tree_based': 'Аналіз AST-дерев', 'graph_based': 'Аналіз графів (CFG/PDG)', 'metrics_based': 'Метрики коду' };
  return map[name] || name;
};

const formatMethodName = (method: string) => {
  const map: Record<string, string> = { 'levenshtein': 'Відстань Левенштейна', 'line_matching': 'Порядкове Порівняння', 'rabin_karp': 'Алгоритм Рабіна-Карпа', 'ngram_search': 'Пошук за N-грамами', 'jaccard_token': 'Токени Джаккарда', 'winnowing': 'Вінновінг (Winnowing)', 'ast_hashing': 'Хешування AST', 'ast_compare': 'Пряме Порівняння AST', 'subtree_isomorphism': 'Ізоморфізм Піддерев', 'cfg': 'Граф потоку керування (CFG)', 'pdg': 'Граф залежностей даних (PDG)', 'subgraph_isomorphism': 'Ізоморфізм підграфів', 'halstead': 'Метрики Холстеда', 'mccabe': 'Складність Маккейба' };
  return map[method] || method.replace(/_/g, ' ');
};

const formatMatchType = (type: string) => {
  const map: Record<string, string> = { 'Identical Subtree Found': 'Знайдено ідентичне піддерево', 'Full Structure Match': 'Повний структурний збіг', 'CFG Node Match': 'Збіг вузлів CFG', 'Data Dependency Match': 'Збіг залежностей даних' };
  return map[type] || type;
};

const formatSeverity = (severity: string) => {
  const map: Record<string, string> = { 'high': 'Високий', 'med': 'Середній', 'low': 'Низький' };
  return map[severity] || severity;
};

const formatMetricKey = (key: string) => {
  const map: Record<string, string> = { 'cyclomatic_complexity': 'Цикломатична складність', 'complexity': 'Цикломатична складність', 'halstead_effort': 'Зусилля (Halstead)', 'halstead_volume': 'Об\'єм (Halstead)', 'volume': 'Об\'єм (Halstead)', 'halstead_difficulty': 'Складність (Halstead)', 'difficulty': 'Складність (Halstead)', 'halstead_bugs': 'Очікувані помилки (Halstead)', 'maintainability_index': 'Індекс підтримуваності', 'loc': 'Кількість рядків коду (LOC)' };
  return map[key] || String(key).replace(/_/g, ' ');
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
       if (lineContent) result.push(`: ${lineContent.trim()}`);
     });
  }
  return result.join('\n');
};

const hasOccurrence = (hash: MatchedHash, sub: 'a' | 'b') => {
  return hash.occurrences?.some(o => o.submission === sub);
};

const getMatchedASTNodeIds = (evidence: Evidence | undefined, fileId: 'a' | 'b'): Set<string> => {
  const ids = new Set<string>();
  if (evidence?.matched_subtrees) {
    evidence.matched_subtrees.forEach(st => {
      const nodes = fileId === 'a' ? st.nodes_a : st.nodes_b;
      if (nodes) nodes.forEach(n => ids.add(n.id));
    });
  }
  return ids;
};

const buildASTVisData = (nodes: ASTNode[], matchedNodeIds?: Set<string>): { nodes: VisNode[], edges: VisEdge[] } => {
  const visNodes: VisNode[] = [];
  const visEdges: VisEdge[] = [];
  if (!nodes) return { nodes: visNodes, edges: visEdges };
  nodes.forEach((n: ASTNode) => {
    const nodeObj: VisNode = { id: n.id, label: (n.label || '').replace(/[\[\]{}()<>]/g, ' ') };
    const isMatched = matchedNodeIds && matchedNodeIds.has(n.id);
    if (isMatched) {
      nodeObj.color = {
          background: '#e8f5e9',
          border: '#4caf50',
          highlight: { background: '#c8e6c9', border: '#4caf50' }
      };
      nodeObj.font = { color: '#2e7d32' };
      nodeObj.borderWidth = 2;
    }
    visNodes.push(nodeObj);
    if (n.children && Array.isArray(n.children)) {
      n.children.forEach((cId: string) => {
        const edgeObj: VisEdge = { from: n.id, to: cId };
        if (isMatched && matchedNodeIds.has(cId)) {
            edgeObj.color = { color: '#4caf50' };
            edgeObj.width = 2;
        }
        visEdges.push(edgeObj);
      });
    }
  });
  return { nodes: visNodes, edges: visEdges };
};

const buildCFGVisData = (graph: CFGGraph | undefined): { nodes: VisNode[], edges: VisEdge[] } => {
  const visNodes: VisNode[] = [];
  const visEdges: VisEdge[] = [];
  if (!graph || !graph.nodes) return { nodes: visNodes, edges: visEdges };
  graph.nodes.forEach((n: CFGNode) => {
    let content = (n.content || '').replace(/["\]/g, "'").replace(/[\[\]{}()<>]/g, ' ');
    if (content.length > 40) content = content.substring(0, 40) + '...';
    visNodes.push({ id: n.id, label: `[L${n.line}]\n`, group: n.type });
  });
  if (graph.edges && Array.isArray(graph.edges)) {
    graph.edges.forEach((e: CFGEdge) => visEdges.push({ from: e.source, to: e.target, label: e.type || '' }));
  }
  return { nodes: visNodes, edges: visEdges };
};

const printGraphOptions: VisOptions = {
  autoResize: true,
  interaction: {
    dragNodes: false,
    dragView: false,
    zoomView: false,
    selectable: false
  },
  physics: {
    enabled: true,
    hierarchicalRepulsion: { nodeDistance: 200 }
  },
  nodes: {
    shape: 'box',
    margin: { top: 15, right: 15, bottom: 15, left: 15 },
    font: { color: '#212529', face: 'monospace', size: 18 },
    color: { background: '#ffffff', border: '#ced4da' },
    borderWidth: 2,
    shadow: false
  },
  edges: {
    arrows: 'to',
    color: { color: '#adb5bd' },
    smooth: { enabled: true, type: 'cubicBezier', roundness: 0.5 },
    width: 2
  },
  layout: {
    hierarchical: { enabled: true, direction: 'UD', sortMethod: 'directed', levelSeparation: 150 }
  }
};

const downloadPdf = async () => {
  if (!reportContainer.value || !report.value) return;

  isGenerating.value = true;

  if (!props.hiddenRender) {
    window.scrollTo(0, 0);
  }
  await new Promise(resolve => setTimeout(resolve, 1500));

  try {
    const opt = {
      margin:       [10, 0, 10, 0] as [number, number, number, number],
      filename:     `AlgoTrace_Report_${report.value.analysis_id}.pdf`,
      image:        { type: 'jpeg' as const, quality: 0.98 },
      html2canvas:  { scale: 1.2, useCORS: true, scrollY: 0, backgroundColor: '#ffffff' },
      jsPDF:        { unit: 'mm' as const, format: 'a4' as const, orientation: 'portrait' as const },
      pagebreak:    { mode: ['css', 'legacy'], avoid: '.avoid-break' }
    };

    await html2pdf().set(opt).from(reportContainer.value).save();
  } catch (error) {
    console.error("Помилка генерації PDF:", error);
    if (!props.hiddenRender) alert("Не вдалося згенерувати PDF-файл.");
  } finally {
    isGenerating.value = false;
    emit('pdf-generated');
  }
};
</script>

<template>
  <div class="print-container bg-white" :class="{ 'min-vh-100': !hiddenRender }" v-if="report">
    <!-- Controls (Hidden in print) -->
    <div v-if="!hiddenRender" class="no-print p-3 bg-light border-bottom sticky-top d-flex justify-content-between align-items-center z-3 shadow-sm">
      <button @click="router.back()" class="btn btn-outline-secondary rounded-pill px-4 fw-bold">
        <i class="bi bi-arrow-left me-2"></i> Назад до результатів
      </button>
      <h5 class="m-0 fw-bold text-dark d-none d-md-block">Попередній перегляд звіту PDF</h5>
      <button @click="downloadPdf" :disabled="isGenerating" class="btn btn-danger rounded-pill px-4 fw-bold shadow-sm d-flex align-items-center">
        <span v-if="isGenerating" class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
        <i v-else class="bi bi-file-earmark-arrow-down-fill me-2"></i>
        {{ isGenerating ? 'Генерація файлу...' : 'Завантажити PDF' }}
      </button>
    </div>

    <div class="container py-5 px-4" style="max-width: 1000px;" ref="reportContainer">

      <!-- Report Header -->
      <div class="text-center mb-5 avoid-break">
        <div class="d-inline-flex align-items-center justify-content-center p-3 rounded-circle bg-primary bg-opacity-10 mb-3">
          <i class="bi bi-layers-half text-primary fs-1"></i>
        </div>
        <h1 class="fw-black text-dark tracking-tight mb-2">Звіт перевірки коду на схожість</h1>
        <p class="text-muted font-monospace small mb-4">ID Аналізу: {{ report.analysis_id }}</p>

        <div class="row g-4 justify-content-center">
          <div class="col-md-5">
            <div class="p-4 border rounded-4 bg-light h-100 d-flex flex-column align-items-center justify-content-center">
              <span class="text-muted text-uppercase fw-bold small tracking-wider mb-2">Файл 1</span>
              <span class="fw-bold fs-5 text-dark text-break">{{ report.source_files.name_a }}</span>
            </div>
          </div>
          <div class="col-md-2 d-flex align-items-center justify-content-center">
             <div class="text-center">
                <span class="d-block fw-bold text-uppercase text-muted small mb-2">Схожість</span>
                <h2 class="fw-black m-0" :style="`color: ${getScoreColorHex(globalScore)}`">{{ globalScore }}%</h2>
             </div>
          </div>
          <div class="col-md-5">
            <div class="p-4 border rounded-4 bg-light h-100 d-flex flex-column align-items-center justify-content-center">
              <span class="text-muted text-uppercase fw-bold small tracking-wider mb-2">Файл 2</span>
              <span class="fw-bold fs-5 text-dark text-break">{{ report.source_files.name_b }}</span>
            </div>
          </div>
        </div>
      </div>

      <hr class="mb-5">

      <!-- Categories & Algorithms Details -->
      <div v-for="cat in report.categories_results" :key="cat.category_name" class="mb-5">

        <div class="d-flex align-items-center justify-content-between bg-light p-3 rounded-3 mb-4 border avoid-break">
           <h3 class="m-0 fw-bold text-dark d-flex align-items-center">
             <i class="bi bi-folder2-open text-primary me-3"></i> {{ formatCategoryName(cat.category_name) }}
           </h3>
           <span class="badge border px-3 py-2 fs-6 rounded-pill text-dark bg-white shadow-sm">Схожість: {{ formatScore(cat.category_similarity_score) }}</span>
        </div>

        <div v-for="algo in cat.algorithms" :key="algo.method" class="mb-5 ms-0 ms-md-4 border-start border-4 border-primary border-opacity-25 ps-4 pb-4">

           <div class="d-flex align-items-center justify-content-between mb-3 avoid-break">
             <h4 class="fw-bold text-dark mb-0 d-flex align-items-center">
               <i class="bi bi-cpu text-secondary me-2"></i> {{ formatMethodName(algo.method) }}
             </h4>
             <span class="badge px-3 py-2 bg-light border text-dark fs-6 rounded-pill">Збіг: {{ formatScore(algo.similarity_score) }}</span>
           </div>

           <!-- Text Highlight Unrolled -->
           <div v-if="algo.evidence_type === 'text_highlight' && algo.evidence?.matched_blocks" class="mt-4">
             <div v-for="(block, bIdx) in algo.evidence.matched_blocks" :key="bIdx" class="mb-4 border rounded-4 overflow-hidden shadow-sm">
                <div class="bg-light px-4 py-2 border-bottom fw-bold text-muted small d-flex align-items-center">
                  <i class="bi bi-file-diff text-primary me-2"></i> Збіг #{{ bIdx + 1 }}
                  <span class="badge bg-white text-secondary border ms-auto">Рядки: {{ block.start_line_a }}-{{ block.end_line_a }} ⟷ {{ block.start_line_b }}-{{ block.end_line_b }}</span>
                </div>
                <div class="row g-0">
                  <div class="col-6 border-end">
                    <div class="bg-light px-3 py-1 border-bottom small fw-bold text-secondary text-truncate">{{ block.file_a || report.source_files.name_a }}</div>
                    <pre class="m-0 p-3 bg-white text-danger code-block"><code>{{ block.content_a }}</code></pre>
                  </div>
                  <div class="col-6">
                    <div class="bg-light px-3 py-1 border-bottom small fw-bold text-secondary text-truncate">{{ block.file_b || report.source_files.name_b }}</div>
                    <pre class="m-0 p-3 bg-white text-danger code-block"><code>{{ block.content_b }}</code></pre>
                  </div>
                </div>
             </div>
             <div v-if="algo.evidence.matched_blocks.length === 0" class="text-muted small italic">Збігів не знайдено.</div>
           </div>

           <!-- Token Sequence Unrolled -->
           <div v-else-if="algo.evidence_type === 'token_sequence' && algo.evidence?.matched_hashes" class="mt-4">
              <table class="table table-bordered bg-white shadow-sm rounded-4 overflow-hidden align-middle">
                <thead class="bg-light text-muted small text-uppercase">
                  <tr>
                    <th>Хеш послідовності</th>
                    <th class="text-center">Файл 1</th>
                    <th class="text-center">Файл 2</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(hash, hIdx) in algo.evidence.matched_hashes" :key="hIdx">
                    <td>
                      <span class="badge bg-light text-secondary border me-2 font-monospace">{{ hash.hash_value }}</span>
                      <span class="small">{{ hash.token_sequence }}</span>
                    </td>
                    <td class="text-center">
                       <i class="bi bi-check-circle-fill text-success" v-if="hasOccurrence(hash, 'a')"></i><span v-else class="text-muted">-</span>
                    </td>
                    <td class="text-center">
                       <i class="bi bi-check-circle-fill text-danger" v-if="hasOccurrence(hash, 'b')"></i><span v-else class="text-muted">-</span>
                    </td>
                  </tr>
                  <tr v-if="algo.evidence.matched_hashes.length === 0"><td colspan="3" class="text-center text-muted">Збігів не знайдено</td></tr>
                </tbody>
              </table>
           </div>

           <!-- AST Tree Unrolled -->
           <div v-else-if="algo.evidence_type === 'ast_tree_mapping'" class="mt-4">
              <div v-if="Array.isArray(algo.evidence)">
                  <div v-for="(m, idx) in algo.evidence" :key="idx" class="mb-4 border rounded-4 overflow-hidden shadow-sm">
                     <div class="bg-light px-3 py-2 border-bottom d-flex justify-content-between align-items-center">
                       <span class="small fw-bold text-dark"><i class="bi bi-link-45deg text-primary me-2"></i>{{ formatMatchType(m.type) }}</span>
                       <span class="badge border bg-white text-secondary rounded-pill">Рівень: {{ formatSeverity(m.severity) }}</span>
                     </div>
                     <div class="row g-0" v-if="(m.leftLines && m.leftLines.length) || (m.rightLines && m.rightLines.length)">
                        <div class="col-6 border-end">
                           <div class="bg-light bg-opacity-50 px-3 py-1 border-bottom small text-muted text-truncate">{{ report.source_files.name_a }}</div>
                           <pre class="m-0 p-3 code-block"><code>{{ getLinesContent(report.source_files?.file_a, m.leftLines, 'ast_tree_mapping') }}</code></pre>
                        </div>
                        <div class="col-6">
                           <div class="bg-light bg-opacity-50 px-3 py-1 border-bottom small text-muted text-truncate">{{ report.source_files.name_b }}</div>
                           <pre class="m-0 p-3 code-block text-danger"><code>{{ getLinesContent(report.source_files?.file_b, m.rightLines, 'ast_tree_mapping') }}</code></pre>
                        </div>
                     </div>
                     <div v-else class="p-3 text-muted small text-center bg-white">Структурний збіг для всього файлу</div>
                  </div>
              </div>
              <template v-else>
              <div v-if="algo.evidence?.full_nodes_a && algo.evidence?.full_nodes_b">
                 <div class="html2pdf__page-break"></div> <!-- Примусовий розрив сторінки для AST-графів -->
                 <div class="mb-4 border rounded-4 overflow-hidden shadow-sm p-3 bg-white">
                    <h6 class="fw-bold text-dark mb-3"><i class="bi bi-diagram-3 text-primary me-2"></i> Повне AST-дерево</h6>
                    <div class="row g-4">
                      <div class="col-12 avoid-break">
                         <div class="border rounded-3 h-100">
                           <div class="bg-light px-3 py-2 border-bottom fw-bold text-center">Файл 1 (Вузли)</div>
                           <div class="p-2 graph-print-container d-flex justify-content-center bg-white border-bottom">
                             <InteractiveGraph :graph-data="buildASTVisData(algo.evidence.full_nodes_a, getMatchedASTNodeIds(algo.evidence, 'a'))" :options="printGraphOptions" height="500px" class="border-0 shadow-none" />
                           </div>
                           <ul class="list-group list-group-flush small m-0">
                             <li class="list-group-item px-2 py-1 border-0" v-for="node in algo.evidence.full_nodes_a" :key="node.id">
                               <span class="text-muted font-monospace me-2">[{{ node.id }}]</span>{{ node.label }}
                             </li>
                           </ul>
                         </div>
                      </div>
                      <div class="col-12 avoid-break mt-4">
                         <div class="border rounded-3 h-100">
                           <div class="bg-light px-3 py-2 border-bottom fw-bold text-center">Файл 2 (Вузли)</div>
                           <div class="p-2 graph-print-container d-flex justify-content-center bg-white border-bottom">
                             <InteractiveGraph :graph-data="buildASTVisData(algo.evidence.full_nodes_b, getMatchedASTNodeIds(algo.evidence, 'b'))" :options="printGraphOptions" height="500px" class="border-0 shadow-none" />
                           </div>
                           <ul class="list-group list-group-flush small m-0">
                             <li class="list-group-item px-2 py-1 border-0" v-for="node in algo.evidence.full_nodes_b" :key="node.id">
                               <span class="text-muted font-monospace me-2">[{{ node.id }}]</span>{{ node.label }}
                             </li>
                           </ul>
                         </div>
                      </div>
                    </div>
                 </div>
              </div>
              <div v-if="algo.evidence?.matched_subtrees">
                 <div class="html2pdf__page-break"></div> <!-- Примусовий розрив сторінки для AST-графів -->
                 <div v-for="(subtree, sIdx) in algo.evidence.matched_subtrees" :key="sIdx" class="mb-4 border rounded-4 overflow-hidden shadow-sm p-3 bg-white">
                    <h6 class="fw-bold text-dark mb-3"><i class="bi bi-diagram-3 text-primary me-2"></i> Збіг піддерева: <span class="text-primary">{{ subtree.node_type }}</span></h6>
                    <div class="row g-4">
                      <div class="col-12 avoid-break">
                         <div class="border rounded-3 h-100">
                           <div class="bg-light px-3 py-2 border-bottom fw-bold text-center">Файл 1 (Вузли)</div>
                           <div class="p-2 graph-print-container d-flex justify-content-center bg-white border-bottom">
                             <InteractiveGraph :graph-data="buildASTVisData(subtree.nodes_a, getMatchedASTNodeIds(algo.evidence, 'a'))" :options="printGraphOptions" height="500px" class="border-0 shadow-none" />
                           </div>
                           <ul class="list-group list-group-flush small m-0">
                             <li class="list-group-item px-2 py-1 border-0" v-for="node in subtree.nodes_a" :key="node.id">
                               <span class="text-muted font-monospace me-2">[{{ node.id }}]</span>{{ node.label }}
                             </li>
                           </ul>
                         </div>
                      </div>
                      <div class="col-12 avoid-break mt-4">
                         <div class="border rounded-3 h-100">
                           <div class="bg-light px-3 py-2 border-bottom fw-bold text-center">Файл 2 (Вузли)</div>
                           <div class="p-2 graph-print-container d-flex justify-content-center bg-white border-bottom">
                             <InteractiveGraph :graph-data="buildASTVisData(subtree.nodes_b, getMatchedASTNodeIds(algo.evidence, 'b'))" :options="printGraphOptions" height="500px" class="border-0 shadow-none" />
                           </div>
                           <ul class="list-group list-group-flush small m-0">
                             <li class="list-group-item px-2 py-1 border-0" v-for="node in subtree.nodes_b" :key="node.id">
                               <span class="text-muted font-monospace me-2">[{{ node.id }}]</span>{{ node.label }}
                             </li>
                           </ul>
                         </div>
                      </div>
                    </div>
                 </div>
              </div>
              </template>
           </div>

           <!-- Graph Mapping Unrolled -->
           <div v-else-if="algo.evidence_type === 'graph_mapping'" class="mt-4">
              <div v-if="algo.evidence.matches && algo.evidence.matches.length > 0" class="mb-4">
                <table class="table table-sm table-bordered bg-white small mb-0">
                  <thead class="bg-light text-muted">
                    <tr><th>Тип збігу</th><th>Рівень</th></tr>
                  </thead>
                  <tbody>
                    <tr v-for="(match, idx) in algo.evidence.matches" :key="idx">
                      <td>{{ formatMatchType(match.type) }}</td>
                      <td><span class="badge bg-light text-dark border">{{ formatSeverity(match.severity) }}</span></td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <div class="html2pdf__page-break" v-if="algo.evidence.graph_a || algo.evidence.graph_b"></div> <!-- Примусовий розрив сторінки для CFG-графів -->
              <div class="row g-4" v-if="algo.evidence.graph_a || algo.evidence.graph_b">
                 <div class="col-12 avoid-break" v-if="algo.evidence.graph_a">
                   <div class="border rounded-3 bg-white h-100">
                     <div class="bg-light px-3 py-2 border-bottom fw-bold small text-center">CFG Файл 1</div>
                     <div class="p-2 graph-print-container d-flex justify-content-center border-bottom">
                       <InteractiveGraph :graph-data="buildCFGVisData(algo.evidence.graph_a)" :options="printGraphOptions" height="500px" class="border-0 shadow-none" />
                     </div>
                     <div class="p-3">
                       <div v-for="node in algo.evidence.graph_a.nodes" :key="node.id" class="mb-2">
                         <span class="badge bg-light text-secondary border me-2">{{ node.type }}</span>
                         <code class="text-dark small">{{ node.content }}</code>
                       </div>
                     </div>
                   </div>
                 </div>
                 <div class="col-12 avoid-break mt-4" v-if="algo.evidence.graph_b">
                   <div class="border rounded-3 bg-white h-100">
                     <div class="bg-light px-3 py-2 border-bottom fw-bold small text-center">CFG Файл 2</div>
                     <div class="p-2 graph-print-container d-flex justify-content-center border-bottom">
                       <InteractiveGraph :graph-data="buildCFGVisData(algo.evidence.graph_b)" :options="printGraphOptions" height="500px" class="border-0 shadow-none" />
                     </div>
                     <div class="p-3">
                       <div v-for="node in algo.evidence.graph_b.nodes" :key="node.id" class="mb-2">
                         <span class="badge bg-light text-secondary border me-2">{{ node.type }}</span>
                         <code class="text-dark small">{{ node.content }}</code>
                       </div>
                     </div>
                   </div>
                 </div>
              </div>
           </div>

           <!-- Metrics Unrolled -->
           <div v-else-if="algo.evidence_type === 'metric_comparison'" class="mt-4 avoid-break">
              <div class="row g-4">
                <div class="col-6">
                   <div class="border rounded-4 bg-white p-3 shadow-sm h-100">
                     <h6 class="fw-bold text-primary mb-3 text-center border-bottom pb-2">Метрики: {{ report.source_files.name_a }}</h6>
                     <table class="table table-sm table-borderless small m-0">
                       <tbody>
                         <tr v-for="(val, key) in algo.evidence.metrics_a" :key="key" class="border-bottom">
                           <td class="text-muted py-2">{{ formatMetricKey(String(key)) }}</td>
                           <td class="text-end fw-bold py-2">{{ typeof val === 'number' && !Number.isInteger(val) ? val.toFixed(2) : val }}</td>
                         </tr>
                       </tbody>
                     </table>
                   </div>
                </div>
                <div class="col-6">
                   <div class="border rounded-4 bg-white p-3 shadow-sm h-100">
                     <h6 class="fw-bold text-danger mb-3 text-center border-bottom pb-2">Метрики: {{ report.source_files.name_b }}</h6>
                     <table class="table table-sm table-borderless small m-0">
                       <tbody>
                         <tr v-for="(val, key) in algo.evidence.metrics_b" :key="key" class="border-bottom">
                           <td class="text-muted py-2">{{ formatMetricKey(String(key)) }}</td>
                           <td class="text-end fw-bold py-2">{{ typeof val === 'number' && !Number.isInteger(val) ? val.toFixed(2) : val }}</td>
                         </tr>
                       </tbody>
                     </table>
                   </div>
                </div>
              </div>
              <div v-if="algo.evidence.conclusion" class="mt-4 alert bg-light border text-dark p-4 rounded-4 shadow-sm text-center">
                 <i class="bi bi-robot text-primary fs-3 d-block mb-2"></i>
                 <span class="fw-medium">{{ algo.evidence.conclusion }}</span>
              </div>
           </div>

           <!-- Raw fallback -->
           <div v-else class="mt-4 bg-light p-3 rounded-3 border">
              <pre class="m-0 small text-muted code-block"><code>{{ JSON.stringify(algo.evidence, null, 2) }}</code></pre>
           </div>

        </div>
      </div>

      <!-- Footer -->
      <div class="text-center mt-5 pt-4 border-top avoid-break text-muted small">
        <p class="mb-1 fw-bold">Система перевірки коду AlgoTrace</p>
        <p class="mb-0">Згенеровано: {{ new Date().toLocaleString() }}</p>
      </div>

    </div>
  </div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Fira+Code:wght@400;500;700&family=Inter:wght@400;600;800;900&display=swap');

.print-container {
  font-family: 'Inter', system-ui, sans-serif;
  color: #212529;
  background: #f8f9fa;
}

.code-block {
  font-family: 'Fira Code', monospace;
  font-size: 0.8rem;
  white-space: pre-wrap;
  word-break: break-word;
}

.graph-print-container {
  height: auto;
  min-height: 500px;
}

.tracking-wider { letter-spacing: 0.05em; }
.tracking-tight { letter-spacing: -0.03em; }
.fw-black { font-weight: 900; }

/* Print Styles */
@media print {
  .no-print {
    display: none !important;
  }

  .print-container {
    background: white !important;
  }

  .avoid-break {
    page-break-inside: avoid;
    break-inside: avoid;
  }

  .page-break {
    page-break-before: always;
  }

  .shadow-sm, .shadow, .shadow-lg {
    box-shadow: none !important;
  }

  body {
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
    background: white;
  }

  .bg-light {
    background-color: #f8f9fa !important;
  }

  .bg-primary {
    background-color: #0d6efd !important;
  }

  .text-primary {
    color: #0d6efd !important;
  }

  .text-danger {
    color: #dc3545 !important;
  }

  .border, .border-start, .border-bottom, .border-top, .border-end {
    border-color: #dee2e6 !important;
  }
}
</style>
