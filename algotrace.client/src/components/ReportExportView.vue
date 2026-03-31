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

const similarityStats = computed(() => {
  const results = report.value?.categories_results;
  if (!results) return null;

  const allAlgos: { name: string, score: number }[] = [];

  results.forEach(cat => {
    if (cat.algorithms) {
      cat.algorithms.forEach(algo => {
        allAlgos.push({
          name: formatMethodName(algo.method),
          score: (algo.similarity_score ?? 0) * 100
        });
      });
    }
  });

  const count = allAlgos.length;
  if (count === 0) return null;

  allAlgos.sort((a, b) => a.score - b.score);

  const firstMatch = allAlgos[0];
  const lastMatch = allAlgos[count - 1];

  if (!firstMatch || !lastMatch) return null;

  const minScore = firstMatch.score;
  const maxScore = lastMatch.score;

  const minAlgos = allAlgos
    .filter(a => Math.abs(a.score - minScore) < 0.01)
    .map(a => a.name);

  const maxAlgos = allAlgos
    .filter(a => Math.abs(a.score - maxScore) < 0.01)
    .map(a => a.name);

  const sum = allAlgos.reduce((acc, curr) => acc + curr.score, 0);
  const avgScore = sum / count;

  let medianScore = 0;
  const mid = Math.floor(count / 2);

  const midAlgo = allAlgos[mid];
  const prevMidAlgo = allAlgos[mid - 1];

  if (count % 2 === 0 && midAlgo && prevMidAlgo) {
    medianScore = (prevMidAlgo.score + midAlgo.score) / 2;
  } else if (midAlgo) {
    medianScore = midAlgo.score;
  }

  return {
    min: Math.round(minScore),
    minAlgos,
    max: Math.round(maxScore),
    maxAlgos,
    avg: Math.round(avgScore),
    median: Math.round(medianScore)
  };
});

const getScoreColorHex = (score: number) => {
  if (score < 30) return '#198754'; // Bootstrap success
  if (score < 70) return '#fd7e14'; // Bootstrap orange
  return '#dc3545'; // Bootstrap danger
};

const formatScore = (score: number) => Math.round((score || 0) * 100) + '%';

const formatCategoryName = (name: string) => {
  const map: Record<string, string> = { 'text_based': 'Текстовий аналіз', 'token_based': 'Токенний аналіз', 'tree_based': 'Аналіз AST-дерев', 'graph_based': 'Аналіз графів (CFG/PDG)', 'metrics_based': 'Метрики коду' };
  return map[name] || name;
};

const formatMethodName = (method: string) => {
  const map: Record<string, string> = { 'levenshtein': 'Відстань Левенштейна', 'line_matching': 'Порядкове порівняння', 'rabin_karp': 'Алгоритм Рабіна-Карпа', 'ngram_search': 'Пошук за N-грамами', 'jaccard_token': 'Токени Джаккарда', 'winnowing': 'Вінновінг (Winnowing)', 'ast_hashing': 'Хешування AST', 'ast_compare': 'Пряме порівняння AST', 'subtree_isomorphism': 'Ізоморфізм піддерев', 'cfg': 'Граф потоку керування (CFG)', 'pdg': 'Граф залежностей даних (PDG)', 'subgraph_isomorphism': 'Ізоморфізм підграфів', 'halstead': 'Метрики Холстеда', 'mccabe': 'Складність Маккейба' };
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
      nodeObj.font = { color: '#1b5e20' };
      nodeObj.borderWidth = 1;
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
  interaction: { dragNodes: false, dragView: false, zoomView: false, selectable: false },
  physics: { enabled: true, hierarchicalRepulsion: { nodeDistance: 160 } },
  nodes: {
    shape: 'box',
    margin: { top: 8, right: 10, bottom: 8, left: 10 },
    font: { color: '#333', face: 'monospace', size: 14 },
    color: { background: '#fff', border: '#ccc' },
    borderWidth: 1,
    shadow: false
  },
  edges: {
    arrows: 'to',
    color: { color: '#999' },
    smooth: { enabled: true, type: 'cubicBezier', roundness: 0.5 },
    width: 1
  },
  layout: { hierarchical: { enabled: true, direction: 'UD', sortMethod: 'directed', levelSeparation: 100 } }
};

const downloadPdf = async () => {
  if (!reportContainer.value || !report.value) return;
  isGenerating.value = true;
  if (!props.hiddenRender) window.scrollTo(0, 0);
  await new Promise(resolve => setTimeout(resolve, 1000));

  try {
    const opt = {
      margin:       [10, 10, 10, 10] as [number, number, number, number],
      filename:     `AlgoTrace_Report_${report.value.analysis_id}.pdf`,
      image:        { type: 'jpeg' as const, quality: 0.98 },
      html2canvas:  { scale: 1.5, useCORS: true, scrollY: 0, backgroundColor: '#ffffff' },
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
  <div class="print-container bg-light" :class="{ 'min-vh-100 py-4': !hiddenRender }" v-if="report">
    
    <div v-if="!hiddenRender" class="no-print container mb-4 d-flex justify-content-between align-items-center">
      <button @click="router.back()" class="btn btn-outline-secondary btn-sm fw-medium">
        &larr; Назад
      </button>
      <button @click="downloadPdf" :disabled="isGenerating" class="btn btn-primary btn-sm fw-medium">
        <span v-if="isGenerating" class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
        {{ isGenerating ? 'Генерація...' : 'Завантажити PDF' }}
      </button>
    </div>

    <div class="container document-page bg-white p-4 p-md-5 mx-auto" ref="reportContainer" style="max-width: 900px;">

<div class="border-bottom pb-4 mb-4 avoid-break">
        <div class="d-flex justify-content-between align-items-end mb-4">
          <div>
            <h1 class="h3 fw-bold mb-1 text-dark">Звіт про аналіз схожості коду</h1>
            <div class="text-muted small font-monospace">ID Аналізу: {{ report.analysis_id }}</div>
          </div>
          </div>

        <div class="row g-3 mb-4" v-if="similarityStats">
          <div class="col-3">
            <div class="border p-2 px-3 h-100 rounded-1 text-center bg-light">
              <div class="small text-muted text-uppercase fw-bold mb-1" style="font-size: 0.65rem;">Максимум</div>
              <div class="h4 fw-bold mb-0" :style="`color: ${getScoreColorHex(similarityStats.max)}`">{{ similarityStats.max }}%</div>
              <div class="text-muted text-truncate" style="font-size: 0.65rem;">
                {{ similarityStats.maxAlgos.length === 1 ? similarityStats.maxAlgos[0] : similarityStats.maxAlgos.length + ' алг.' }}
              </div>
            </div>
          </div>
          <div class="col-3">
             <div class="border p-2 px-3 h-100 rounded-1 text-center bg-light">
              <div class="small text-muted text-uppercase fw-bold mb-1" style="font-size: 0.65rem;">Середнє</div>
              <div class="h4 fw-bold mb-0 text-dark">{{ similarityStats.avg }}%</div>
            </div>
          </div>
          <div class="col-3">
             <div class="border p-2 px-3 h-100 rounded-1 text-center bg-light">
              <div class="small text-muted text-uppercase fw-bold mb-1" style="font-size: 0.65rem;">Медіана</div>
              <div class="h4 fw-bold mb-0 text-dark">{{ similarityStats.median }}%</div>
            </div>
          </div>
          <div class="col-3">
            <div class="border p-2 px-3 h-100 rounded-1 text-center bg-light">
              <div class="small text-muted text-uppercase fw-bold mb-1" style="font-size: 0.65rem;">Мінімум</div>
              <div class="h4 fw-bold mb-0" :style="`color: ${getScoreColorHex(similarityStats.min)}`">{{ similarityStats.min }}%</div>
              <div class="text-muted text-truncate" style="font-size: 0.65rem;">
                {{ similarityStats.minAlgos.length === 1 ? similarityStats.minAlgos[0] : similarityStats.minAlgos.length + ' алг.' }}
              </div>
            </div>
          </div>
        </div>
        <div class="row g-3">
          <div class="col-6">
            <div class="border p-3 h-100 rounded-1">
              <div class="small text-muted text-uppercase fw-bold mb-1">Файл 1</div>
              <div class="fw-medium text-break font-monospace small">{{ report.source_files.name_a }}</div>
            </div>
          </div>
          <div class="col-6">
            <div class="border p-3 h-100 rounded-1">
              <div class="small text-muted text-uppercase fw-bold mb-1">Файл 2</div>
              <div class="fw-medium text-break font-monospace small">{{ report.source_files.name_b }}</div>
            </div>
          </div>
        </div>
      </div>

      <div v-for="(cat, cIdx) in report.categories_results" :key="cat.category_name" class="mb-4">
        
        <div class="d-flex justify-content-between align-items-center border-bottom pb-2 mb-3 avoid-break mt-4">
          <h2 class="h5 fw-bold m-0 text-dark">{{ cIdx + 1 }}. {{ formatCategoryName(cat.category_name) }}</h2>
          <span class="badge border bg-light text-dark">Схожість: {{ formatScore(cat.category_similarity_score) }}</span>
        </div>

        <div v-for="algo in cat.algorithms" :key="algo.method" class="mb-4 ps-3 border-start border-2 border-secondary border-opacity-25">
          <div class="d-flex justify-content-between align-items-center mb-2 avoid-break">
            <h3 class="h6 fw-bold m-0 text-secondary">{{ formatMethodName(algo.method) }}</h3>
            <span class="small fw-medium">Збіг: {{ formatScore(algo.similarity_score) }}</span>
          </div>

          <div v-if="algo.evidence_type === 'text_highlight' && algo.evidence?.matched_blocks">
            <div v-for="(block, bIdx) in algo.evidence.matched_blocks" :key="bIdx" class="border rounded-1 mb-3 avoid-break">
              <div class="bg-light px-2 py-1 border-bottom small text-muted d-flex justify-content-between">
                <span>Блок #{{ bIdx + 1 }}</span>
                <span>Рядки: {{ block.start_line_a }}-{{ block.end_line_a }} &rarr; {{ block.start_line_b }}-{{ block.end_line_b }}</span>
              </div>
              <div class="row g-0">
                <div class="col-6 border-end">
                  <pre class="m-0 p-2 code-block bg-white text-dark"><code>{{ block.content_a }}</code></pre>
                </div>
                <div class="col-6">
                  <pre class="m-0 p-2 code-block bg-white text-dark"><code>{{ block.content_b }}</code></pre>
                </div>
              </div>
            </div>
            <div v-if="algo.evidence.matched_blocks.length === 0" class="text-muted small">Збігів не знайдено.</div>
          </div>

          <div v-else-if="algo.evidence_type === 'token_sequence' && algo.evidence?.matched_hashes">
            <table class="table table-sm table-bordered mb-0 avoid-break">
              <thead class="table-light text-muted small">
                <tr>
                  <th>Послідовність токенів</th>
                  <th class="text-center" style="width: 60px">Ф1</th>
                  <th class="text-center" style="width: 60px">Ф2</th>
                </tr>
              </thead>
              <tbody class="small">
                <tr v-for="(hash, hIdx) in algo.evidence.matched_hashes" :key="hIdx">
                  <td class="font-monospace text-break">{{ hash.token_sequence }} <br><span class="text-muted" style="font-size: 0.7rem;">{{ hash.hash_value }}</span></td>
                  <td class="text-center align-middle">{{ hasOccurrence(hash, 'a') ? '✓' : '-' }}</td>
                  <td class="text-center align-middle">{{ hasOccurrence(hash, 'b') ? '✓' : '-' }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div v-else-if="algo.evidence_type === 'ast_tree_mapping'">
            <div v-if="Array.isArray(algo.evidence)">
              <div v-for="(m, idx) in algo.evidence" :key="idx" class="border rounded-1 mb-2 avoid-break">
                <div class="bg-light px-2 py-1 border-bottom small d-flex justify-content-between">
                  <span class="fw-medium">{{ formatMatchType(m.type) }}</span>
                  <span class="text-muted">{{ formatSeverity(m.severity) }}</span>
                </div>
                <div class="row g-0" v-if="(m.leftLines && m.leftLines.length) || (m.rightLines && m.rightLines.length)">
                  <div class="col-6 border-end">
                    <pre class="m-0 p-2 code-block"><code>{{ getLinesContent(report.source_files?.file_a, m.leftLines, 'ast_tree_mapping') }}</code></pre>
                  </div>
                  <div class="col-6">
                    <pre class="m-0 p-2 code-block"><code>{{ getLinesContent(report.source_files?.file_b, m.rightLines, 'ast_tree_mapping') }}</code></pre>
                  </div>
                </div>
                <div v-else class="p-2 text-muted small text-center">Структурний збіг для всього файлу</div>
              </div>
            </div>
            
            <template v-else>
              <div v-if="algo.evidence?.full_nodes_a && algo.evidence?.full_nodes_b">
                <div class="html2pdf__page-break"></div>
                <h4 class="h6 fw-bold mb-2">Повне AST-дерево</h4>
                <div class="border rounded-1 mb-3 avoid-break">
                  <div class="bg-light px-2 py-1 border-bottom small fw-bold">Файл 1</div>
                  <div class="graph-print-container"><InteractiveGraph :graph-data="buildASTVisData(algo.evidence.full_nodes_a, getMatchedASTNodeIds(algo.evidence, 'a'))" :options="printGraphOptions" height="300px" /></div>
                </div>
                <div class="border rounded-1 mb-3 avoid-break">
                  <div class="bg-light px-2 py-1 border-bottom small fw-bold">Файл 2</div>
                  <div class="graph-print-container"><InteractiveGraph :graph-data="buildASTVisData(algo.evidence.full_nodes_b, getMatchedASTNodeIds(algo.evidence, 'b'))" :options="printGraphOptions" height="300px" /></div>
                </div>
              </div>
              
              <div v-if="algo.evidence?.matched_subtrees">
                <div class="html2pdf__page-break"></div>
                <div v-for="(subtree, sIdx) in algo.evidence.matched_subtrees" :key="sIdx" class="mb-4">
                  <h4 class="h6 fw-bold mb-2">Піддерево: {{ subtree.node_type }}</h4>
                  <div class="border rounded-1 mb-2 avoid-break">
                    <div class="bg-light px-2 py-1 border-bottom small fw-bold">Файл 1</div>
                    <div class="graph-print-container"><InteractiveGraph :graph-data="buildASTVisData(subtree.nodes_a, getMatchedASTNodeIds(algo.evidence, 'a'))" :options="printGraphOptions" height="250px" /></div>
                  </div>
                  <div class="border rounded-1 avoid-break">
                    <div class="bg-light px-2 py-1 border-bottom small fw-bold">Файл 2</div>
                    <div class="graph-print-container"><InteractiveGraph :graph-data="buildASTVisData(subtree.nodes_b, getMatchedASTNodeIds(algo.evidence, 'b'))" :options="printGraphOptions" height="250px" /></div>
                  </div>
                </div>
              </div>
            </template>
          </div>

          <div v-else-if="algo.evidence_type === 'graph_mapping'">
            <table class="table table-sm table-bordered mb-3 avoid-break" v-if="algo.evidence.matches && algo.evidence.matches.length > 0">
              <thead class="table-light small">
                <tr><th>Тип збігу</th><th>Рівень</th></tr>
              </thead>
              <tbody class="small">
                <tr v-for="(match, idx) in algo.evidence.matches" :key="idx">
                  <td>{{ formatMatchType(match.type) }}</td>
                  <td>{{ formatSeverity(match.severity) }}</td>
                </tr>
              </tbody>
            </table>

            <div v-if="algo.evidence.graph_a || algo.evidence.graph_b">
              <div class="html2pdf__page-break"></div>
              <div class="border rounded-1 mb-3 avoid-break" v-if="algo.evidence.graph_a">
                <div class="bg-light px-2 py-1 border-bottom small fw-bold">CFG Файл 1</div>
                <div class="graph-print-container border-bottom"><InteractiveGraph :graph-data="buildCFGVisData(algo.evidence.graph_a)" :options="printGraphOptions" height="300px" /></div>
              </div>
              <div class="border rounded-1 avoid-break" v-if="algo.evidence.graph_b">
                <div class="bg-light px-2 py-1 border-bottom small fw-bold">CFG Файл 2</div>
                <div class="graph-print-container border-bottom"><InteractiveGraph :graph-data="buildCFGVisData(algo.evidence.graph_b)" :options="printGraphOptions" height="300px" /></div>
              </div>
            </div>
          </div>

          <div v-else-if="algo.evidence_type === 'metric_comparison'" class="avoid-break">
            <div class="row g-3">
              <div class="col-6">
                <div class="border rounded-1 p-2 h-100">
                  <div class="small fw-bold border-bottom pb-1 mb-2">Метрики: Файл 1</div>
                  <table class="table table-sm table-borderless small mb-0">
                    <tbody>
                      <tr v-for="(val, key) in algo.evidence.metrics_a" :key="key" class="border-bottom border-light">
                        <td class="text-muted p-1">{{ formatMetricKey(String(key)) }}</td>
                        <td class="text-end fw-medium p-1">{{ typeof val === 'number' && !Number.isInteger(val) ? val.toFixed(2) : val }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
              <div class="col-6">
                <div class="border rounded-1 p-2 h-100">
                  <div class="small fw-bold border-bottom pb-1 mb-2">Метрики: Файл 2</div>
                  <table class="table table-sm table-borderless small mb-0">
                    <tbody>
                      <tr v-for="(val, key) in algo.evidence.metrics_b" :key="key" class="border-bottom border-light">
                        <td class="text-muted p-1">{{ formatMetricKey(String(key)) }}</td>
                        <td class="text-end fw-medium p-1">{{ typeof val === 'number' && !Number.isInteger(val) ? val.toFixed(2) : val }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
            <div v-if="algo.evidence.conclusion" class="mt-3 p-3 bg-light border rounded-1 small fw-medium">
              Висновок: {{ algo.evidence.conclusion }}
            </div>
          </div>

          <div v-else class="bg-light p-2 border rounded-1">
            <pre class="m-0 small text-muted code-block"><code>{{ JSON.stringify(algo.evidence, null, 2) }}</code></pre>
          </div>
        </div>
      </div>

      <div class="text-center mt-5 pt-3 border-top avoid-break text-muted small">
        <span class="fw-bold">AlgoTrace Report</span> &bull; Згенеровано: {{ new Date().toLocaleString() }}
      </div>

    </div>
  </div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Fira+Code:wght@400;500&family=Inter:wght@400;500;600;700&display=swap');

.print-container {
  font-family: 'Inter', system-ui, sans-serif;
  color: #1a1d20;
}

/* Document Page Wrapper for Screen */
.document-page {
  box-shadow: 0 4px 12px rgba(0,0,0,0.08);
  border: 1px solid #e0e0e0;
}

.code-block {
  font-family: 'Fira Code', monospace;
  font-size: 0.75rem;
  line-height: 1.4;
  white-space: pre-wrap;
  word-break: break-word;
}

.graph-print-container {
  height: auto;
  min-height: 250px;
  background-color: #fcfcfc;
}

/* Print Specific Styles */
@media print {
  .no-print {
    display: none !important;
  }

  .print-container {
    background: transparent !important;
  }

  .document-page {
    box-shadow: none !important;
    border: none !important;
    padding: 0 !important;
    max-width: 100% !important;
  }

  .avoid-break {
    page-break-inside: avoid;
    break-inside: avoid;
  }

  .page-break {
    page-break-before: always;
  }

  body {
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
    background: white;
  }

  .bg-light {
    background-color: #f8f9fa !important;
  }

  .border, .border-start, .border-bottom, .border-top, .border-end {
    border-color: #dee2e6 !important;
  }
}
</style>