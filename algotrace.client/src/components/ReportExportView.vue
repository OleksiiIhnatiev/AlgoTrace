<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router';
import { analysisState } from '@/services/analysis.service';
import { computed, onMounted, ref, nextTick } from 'vue';
import InteractiveGraph from './InteractiveGraph.vue';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';
import { isDarkMode } from '../composables/useTheme';

// Глобальний патч для уникнення willReadFrequently
if (typeof window !== 'undefined' && HTMLCanvasElement) {
  const origGetContext = HTMLCanvasElement.prototype.getContext;
  if (!(origGetContext as any).__patched) {
    HTMLCanvasElement.prototype.getContext = function(type: string, attributes?: any) {
      if (type === '2d') {
        attributes = { ...(attributes || {}), willReadFrequently: true };
      }
      return origGetContext.call(this, type, attributes);
    };
    (HTMLCanvasElement.prototype.getContext as any).__patched = true;
  }
}

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

interface VisNode {
  id: string | number;
  label?: string;
  color?: any;
  font?: any;
  borderWidth?: number;
}

interface VisEdge {
  from: string | number;
  to: string | number;
  label?: string;
  color?: any;
  width?: number;
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

// Синхронізовано з палітрою AnalyzerView
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
          background: '#ecfdf5',
          border: '#10b981',
          highlight: { background: '#d1fae5', border: '#059669' }
      };
      nodeObj.font = { color: '#047857' };
      nodeObj.borderWidth = 2;
    }
    visNodes.push(nodeObj);
    if (n.children && Array.isArray(n.children)) {
      n.children.forEach((cId: string) => {
        const edgeObj: VisEdge = { from: n.id, to: cId };
        if (isMatched && matchedNodeIds.has(cId)) {
            edgeObj.color = { color: '#10b981' };
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
    let content = (n.content || '').replace(/["\\]/g, "'").replace(/[\[\]{}()<>]/g, ' ');
    if (content.length > 40) content = content.substring(0, 40) + '...';
    visNodes.push({ id: n.id, label: `[L${n.line}]\n${content}` });
  });
  if (graph.edges && Array.isArray(graph.edges)) {
    graph.edges.forEach((e: CFGEdge) => visEdges.push({ from: e.source, to: e.target, label: e.type || '' }));
  }
  return { nodes: visNodes, edges: visEdges };
};

const downloadPdf = async () => {
  if (!reportContainer.value || !report.value) return;
  isGenerating.value = true;
  if (!props.hiddenRender) window.scrollTo(0, 0);
  
  await new Promise(resolve => setTimeout(resolve, 2000));

  try {
    const pdf = new jsPDF('p', 'mm', 'a4');
    const pdfWidth = pdf.internal.pageSize.getWidth();
    const pdfHeight = pdf.internal.pageSize.getHeight();
    const margin = 12;
    const maxContentWidth = pdfWidth - margin * 2;
    const maxContentHeight = pdfHeight - margin * 2;
    let currentY = margin;

    const chunks = reportContainer.value.querySelectorAll('.pdf-chunk');

    for (let i = 0; i < chunks.length; i++) {
      const chunk = chunks[i] as HTMLElement;
      if (chunk.offsetHeight === 0) continue;

      const canvas = await html2canvas(chunk, { 
        scale: 2, 
        useCORS: true, 
        logging: false, 
        backgroundColor: isDarkMode.value ? '#212529' : '#ffffff',
        onclone: (clonedDoc: any) => {
          const win = clonedDoc.defaultView;
          if (win && win.HTMLCanvasElement) {
            const origGetContext = win.HTMLCanvasElement.prototype.getContext;
            if (!(origGetContext as any).__patched) {
              win.HTMLCanvasElement.prototype.getContext = function(type: string, attributes?: any) {
                if (type === '2d') {
                  attributes = { ...(attributes || {}), willReadFrequently: true };
                }
                return origGetContext.call(this, type, attributes);
              };
              (win.HTMLCanvasElement.prototype.getContext as any).__patched = true;
            }
          }
        }
      });
      if (canvas.width === 0 || canvas.height === 0) continue;

      const imgData = canvas.toDataURL('image/jpeg', 0.95);
      if (!imgData || imgData === 'data:,' || !imgData.startsWith('data:image')) continue;

      const imgProps = pdf.getImageProperties(imgData);
      const imgWidth = maxContentWidth;
      const imgHeight = (imgProps.height * imgWidth) / imgProps.width;

      if (currentY + imgHeight > pdfHeight - margin && currentY > margin + 5) {
        pdf.addPage();
        currentY = margin;
      }

      if (imgHeight > maxContentHeight) {
        let heightLeft = imgHeight;
        let position = 0;
        while (heightLeft > 0) {
          pdf.addImage(imgData, 'JPEG', margin, currentY - position, imgWidth, imgHeight);
          heightLeft -= maxContentHeight;
          position += maxContentHeight;
          if (heightLeft > 0) { pdf.addPage(); currentY = margin; }
        }
        currentY = margin;
      } else {
        pdf.addImage(imgData, 'JPEG', margin, currentY, imgWidth, imgHeight);
        currentY += imgHeight + 4;
      }
    }
    pdf.save(`AlgoTrace_Report_${report.value.analysis_id}.pdf`);
  } catch (error) {
    console.error("Помилка генерації PDF:", error);
  } finally {
    isGenerating.value = false;
    emit('pdf-generated');
  }
};
</script>

<template>
  <div class="elegant-wrapper" :class="{ 'min-vh-100 py-5': !hiddenRender }" v-if="report">
    
    <div v-if="!hiddenRender" class="no-print container mb-4 d-flex justify-content-between align-items-center" style="max-width: 960px;">
      <button @click="router.back()" class="btn-modern-ghost">
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="me-2"><line x1="19" y1="12" x2="5" y2="12"></line><polyline points="12 19 5 12 12 5"></polyline></svg>
        Назад
      </button>
      <button @click="downloadPdf" :disabled="isGenerating" class="btn-modern-primary">
        <span v-if="isGenerating" class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
        {{ isGenerating ? 'Генерація...' : 'Завантажити PDF' }}
      </button>
    </div>

    <div class="container document-page p-4 p-md-5 mx-auto" ref="reportContainer" style="max-width: 960px;">

      <div class="pdf-section pb-4 mb-5 pdf-chunk border-bottom-soft">
        <div class="d-flex justify-content-between align-items-start mb-4">
          <div>
            <h1 class="report-title">Звіт про аналіз схожості коду</h1>
            <div class="report-meta">ID Аналізу: <span class="font-monospace text-dark">{{ report.analysis_id }}</span></div>
          </div>
          <div class="brand-badge">AlgoTrace</div>
        </div>

        <div class="row g-4 mb-4" v-if="similarityStats">
          <div class="col-6 col-md-3">
            <div class="stat-card">
              <div class="stat-label">Максимум</div>
              <div class="stat-value" :style="`color: ${getScoreColorHex(similarityStats.max)}`">{{ similarityStats.max }}%</div>
              <div class="stat-desc">
                {{ similarityStats.maxAlgos.length === 1 ? similarityStats.maxAlgos[0] : similarityStats.maxAlgos.length + ' алг.' }}
              </div>
            </div>
          </div>
          <div class="col-6 col-md-3">
             <div class="stat-card">
              <div class="stat-label">Середнє</div>
              <div class="stat-value text-dark">{{ similarityStats.avg }}%</div>
              <div class="stat-desc">Загальний тренд</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
             <div class="stat-card">
              <div class="stat-label">Медіана</div>
              <div class="stat-value text-dark">{{ similarityStats.median }}%</div>
              <div class="stat-desc">Центральне знач.</div>
            </div>
          </div>
          <div class="col-6 col-md-3">
            <div class="stat-card">
              <div class="stat-label">Мінімум</div>
              <div class="stat-value" :style="`color: ${getScoreColorHex(similarityStats.min)}`">{{ similarityStats.min }}%</div>
              <div class="stat-desc">
                {{ similarityStats.minAlgos.length === 1 ? similarityStats.minAlgos[0] : similarityStats.minAlgos.length + ' алг.' }}
              </div>
            </div>
          </div>
        </div>

        <div class="row g-4">
          <div class="col-md-6">
            <div class="file-card">
              <div class="file-label">Вихідний файл 1</div>
              <div class="file-name">{{ report.source_files.name_a }}</div>
            </div>
          </div>
          <div class="col-md-6">
            <div class="file-card">
              <div class="file-label">Файл для порівняння 2</div>
              <div class="file-name">{{ report.source_files.name_b }}</div>
            </div>
          </div>
        </div>
      </div>

      <div v-for="(cat, cIdx) in report.categories_results" :key="cat.category_name" class="mb-5">
        
        <div class="pdf-section d-flex justify-content-between align-items-center mb-4 pdf-chunk category-header">
          <h2 class="category-title">{{ cIdx + 1 }}. {{ formatCategoryName(cat.category_name) }}</h2>
          <span class="score-pill">Схожість: <strong>{{ formatScore(cat.category_similarity_score) }}</strong></span>
        </div>

        <div v-for="algo in cat.algorithms" :key="algo.method" class="pdf-section mb-5 ms-3 ps-4 algo-section">
          <div class="d-flex justify-content-between align-items-center mb-3 pdf-chunk">
            <h3 class="algo-title">{{ formatMethodName(algo.method) }}</h3>
            <span class="algo-score">Збіг: {{ formatScore(algo.similarity_score) }}</span>
          </div>

          <div v-if="algo.evidence_type === 'text_highlight' && algo.evidence?.matched_blocks">
            <div v-for="(block, bIdx) in algo.evidence.matched_blocks" :key="bIdx" class="evidence-card mb-4 pdf-chunk">
              <div class="evidence-header d-flex justify-content-between">
                <span class="fw-semibold text-dark">Блок #{{ bIdx + 1 }}</span>
                <span>Рядки: {{ block.start_line_a }}-{{ block.end_line_a }} &rarr; {{ block.start_line_b }}-{{ block.end_line_b }}</span>
              </div>
              <div class="row g-0">
                <div class="col-6 border-end-soft">
                  <pre class="code-block"><code>{{ block.content_a }}</code></pre>
                </div>
                <div class="col-6">
                  <pre class="code-block"><code>{{ block.content_b }}</code></pre>
                </div>
              </div>
            </div>
            <div v-if="algo.evidence.matched_blocks.length === 0" class="empty-state pdf-chunk">Збігів не знайдено.</div>
          </div>

          <div v-else-if="algo.evidence_type === 'token_sequence' && algo.evidence?.matched_hashes">
            <div class="pdf-chunk modern-table-wrapper">
              <table class="modern-table">
              <thead>
                <tr>
                  <th>Послідовність токенів</th>
                  <th class="text-center" style="width: 80px">Файл 1</th>
                  <th class="text-center" style="width: 80px">Файл 2</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(hash, hIdx) in algo.evidence.matched_hashes" :key="hIdx">
                  <td class="font-monospace token-cell">
                    {{ hash.token_sequence }} <br>
                    <span class="hash-subtext">{{ hash.hash_value }}</span>
                  </td>
                  <td class="text-center align-middle">
                    <span v-if="hasOccurrence(hash, 'a')" class="check-icon">✓</span>
                    <span v-else class="dash-icon">—</span>
                  </td>
                  <td class="text-center align-middle">
                    <span v-if="hasOccurrence(hash, 'b')" class="check-icon">✓</span>
                    <span v-else class="dash-icon">—</span>
                  </td>
                </tr>
              </tbody>
            </table>
            </div>
          </div>

          <div v-else-if="algo.evidence_type === 'ast_tree_mapping'">
            <div v-if="Array.isArray(algo.evidence)">
              <div v-for="(m, idx) in algo.evidence" :key="idx" class="evidence-card mb-3 pdf-chunk">
                <div class="evidence-header d-flex justify-content-between">
                  <span class="fw-semibold text-dark">{{ formatMatchType(m.type) }}</span>
                  <span class="badge-soft">{{ formatSeverity(m.severity) }}</span>
                </div>
                <div class="row g-0" v-if="(m.leftLines && m.leftLines.length) || (m.rightLines && m.rightLines.length)">
                  <div class="col-6 border-end-soft">
                    <pre class="code-block"><code>{{ getLinesContent(report.source_files?.file_a, m.leftLines, 'ast_tree_mapping') }}</code></pre>
                  </div>
                  <div class="col-6">
                    <pre class="code-block"><code>{{ getLinesContent(report.source_files?.file_b, m.rightLines, 'ast_tree_mapping') }}</code></pre>
                  </div>
                </div>
                <div v-else class="empty-state">Структурний збіг для всього файлу</div>
              </div>
            </div>
            
            <template v-else>
              <div v-if="algo.evidence?.full_nodes_a && algo.evidence?.full_nodes_b">
                <h4 class="sub-heading pdf-chunk">Повне AST-дерево</h4>
                <div class="evidence-card mb-4 pdf-chunk">
                  <div class="evidence-header fw-semibold">Файл 1</div>
                  <div class="graph-print-container"><InteractiveGraph :graph-data="buildASTVisData(algo.evidence.full_nodes_a, getMatchedASTNodeIds(algo.evidence, 'a'))" height="300px" /></div>
                </div>
                <div class="evidence-card mb-4 pdf-chunk">
                  <div class="evidence-header fw-semibold">Файл 2</div>
                  <div class="graph-print-container"><InteractiveGraph :graph-data="buildASTVisData(algo.evidence.full_nodes_b, getMatchedASTNodeIds(algo.evidence, 'b'))" height="300px" /></div>
                </div>
              </div>
              
              <div v-if="algo.evidence?.matched_subtrees">
                <div v-for="(subtree, sIdx) in algo.evidence.matched_subtrees" :key="sIdx" class="mb-5">
                  <h4 class="sub-heading pdf-chunk">Піддерево: <span class="text-primary-soft">{{ subtree.node_type }}</span></h4>
                  <div class="evidence-card mb-3 pdf-chunk">
                    <div class="evidence-header fw-semibold">Файл 1</div>
                    <div class="graph-print-container"><InteractiveGraph :graph-data="buildASTVisData(subtree.nodes_a, getMatchedASTNodeIds(algo.evidence, 'a'))" height="250px" /></div>
                  </div>
                  <div class="evidence-card pdf-chunk">
                    <div class="evidence-header fw-semibold">Файл 2</div>
                    <div class="graph-print-container"><InteractiveGraph :graph-data="buildASTVisData(subtree.nodes_b, getMatchedASTNodeIds(algo.evidence, 'b'))" height="250px" /></div>
                  </div>
                </div>
              </div>
            </template>
          </div>

          <div v-else-if="algo.evidence_type === 'graph_mapping'">
            <div class="pdf-chunk modern-table-wrapper mb-4" v-if="algo.evidence.matches && algo.evidence.matches.length > 0">
              <table class="modern-table">
              <thead>
                <tr><th>Тип збігу</th><th>Рівень</th></tr>
              </thead>
              <tbody>
                <tr v-for="(match, idx) in algo.evidence.matches" :key="idx">
                  <td class="fw-medium text-dark">{{ formatMatchType(match.type) }}</td>
                  <td><span class="badge-soft">{{ formatSeverity(match.severity) }}</span></td>
                </tr>
              </tbody>
            </table>
            </div>

            <div v-if="algo.evidence.graph_a || algo.evidence.graph_b">
              <div class="evidence-card mb-4 pdf-chunk" v-if="algo.evidence.graph_a">
                <div class="evidence-header fw-semibold">CFG Файл 1</div>
                <div class="graph-print-container"><InteractiveGraph :graph-data="buildCFGVisData(algo.evidence.graph_a)" :is-graph="true" height="300px" /></div>
              </div>
              <div class="evidence-card pdf-chunk" v-if="algo.evidence.graph_b">
                <div class="evidence-header fw-semibold">CFG Файл 2</div>
                <div class="graph-print-container"><InteractiveGraph :graph-data="buildCFGVisData(algo.evidence.graph_b)" :is-graph="true" height="300px" /></div>
              </div>
            </div>
          </div>

          <div v-else-if="algo.evidence_type === 'metric_comparison'" class="pdf-chunk">
            <div class="row g-4">
              <div class="col-md-6">
                <div class="metric-card h-100">
                  <div class="metric-header">Метрики: Файл 1</div>
                  <table class="modern-table table-borderless">
                    <tbody>
                      <tr v-for="(val, key) in algo.evidence.metrics_a" :key="key">
                        <td class="metric-key">{{ formatMetricKey(String(key)) }}</td>
                        <td class="metric-val">{{ typeof val === 'number' && !Number.isInteger(val) ? val.toFixed(2) : val }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
              <div class="col-md-6">
                <div class="metric-card h-100">
                  <div class="metric-header">Метрики: Файл 2</div>
                  <table class="modern-table table-borderless">
                    <tbody>
                      <tr v-for="(val, key) in algo.evidence.metrics_b" :key="key">
                        <td class="metric-key">{{ formatMetricKey(String(key)) }}</td>
                        <td class="metric-val">{{ typeof val === 'number' && !Number.isInteger(val) ? val.toFixed(2) : val }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
            <div v-if="algo.evidence.conclusion" class="conclusion-box mt-4">
              <strong class="text-dark me-2">Висновок:</strong> {{ algo.evidence.conclusion }}
            </div>
          </div>

          <div v-else class="evidence-card pdf-chunk">
            <pre class="code-block m-0"><code>{{ JSON.stringify(algo.evidence, null, 2) }}</code></pre>
          </div>
        </div>
      </div>

      <div class="pdf-section text-center mt-5 pt-4 border-top-soft text-muted small pdf-chunk footer-text">
        <span class="fw-bold text-dark">AlgoTrace Report</span> &bull; Згенеровано: {{ new Date().toLocaleString() }}
      </div>

    </div>
  </div>
</template>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Fira+Code:wght@400;500&family=Inter:wght@400;500;600;700&display=swap');

/* Глобальный светлый фон-подложка */
.elegant-wrapper {
  background-color: #f8fafc; /* Очень мягкий серо-синий фон */
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
  color: #334155;
}

/* Основной лист документа */
.document-page {
  background-color: #ffffff;
  border-radius: 16px;
  box-shadow: 0 10px 40px -10px rgba(0,0,0,0.05), 0 1px 3px rgba(0,0,0,0.02);
  border: 1px solid #f1f5f9;
}

/* Кнопки */
.btn-modern-ghost {
  display: inline-flex;
  align-items: center;
  padding: 8px 16px;
  background: transparent;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  color: #475569;
  font-weight: 500;
  font-size: 0.875rem;
  transition: all 0.2s ease;
}
.btn-modern-ghost:hover {
  background: #f1f5f9;
  color: #0f172a;
}

.btn-modern-primary {
  display: inline-flex;
  align-items: center;
  padding: 8px 16px;
  background: #0f172a; /* Глубокий slate для контраста в светлой теме */
  border: 1px solid #0f172a;
  border-radius: 8px;
  color: #ffffff;
  font-weight: 500;
  font-size: 0.875rem;
  transition: all 0.2s ease;
}
.btn-modern-primary:hover {
  background: #334155;
  border-color: #334155;
}

/* Типографика */
.report-title {
  font-size: 1.75rem;
  font-weight: 700;
  letter-spacing: -0.02em;
  color: #0f172a;
  margin-bottom: 0.25rem;
}
.report-meta {
  color: #64748b;
  font-size: 0.875rem;
}
.brand-badge {
  background: #f1f5f9;
  color: #334155;
  padding: 6px 12px;
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

/* Линии */
.border-bottom-soft {
  border-bottom: 1px solid #e2e8f0;
}
.border-top-soft {
  border-top: 1px solid #e2e8f0;
}
.border-end-soft {
  border-right: 1px solid #e2e8f0;
}

/* Статистика */
.stat-card {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 1.25rem;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  transition: transform 0.2s ease;
}
.stat-label {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #64748b;
  font-weight: 600;
  margin-bottom: 0.5rem;
}
.stat-value {
  font-size: 1.875rem;
  font-weight: 700;
  line-height: 1;
  margin-bottom: 0.25rem;
}
.stat-desc {
  font-size: 0.75rem;
  color: #94a3b8;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  width: 100%;
}

/* Карточки файлов */
.file-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 1.25rem;
}
.file-label {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #64748b;
  font-weight: 600;
  margin-bottom: 0.5rem;
}
.file-name {
  font-family: 'Fira Code', monospace;
  font-size: 0.875rem;
  color: #0f172a;
  word-break: break-all;
}

/* Секции категорий */
.category-header {
  padding-bottom: 0.75rem;
  border-bottom: 2px solid #f1f5f9;
}
.category-title {
  font-size: 1.25rem;
  font-weight: 600;
  color: #0f172a;
  margin: 0;
}
.score-pill {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  padding: 4px 12px;
  border-radius: 9999px;
  font-size: 0.875rem;
  color: #475569;
}

/* Алгоритмы */
.algo-section {
  border-left: 3px solid #e2e8f0;
}
.algo-title {
  font-size: 1rem;
  font-weight: 600;
  color: #334155;
  margin: 0;
}
.algo-score {
  font-size: 0.875rem;
  font-weight: 500;
  color: #64748b;
}

/* Карточки контента (Evidence) */
.evidence-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  overflow: hidden;
}
.evidence-header {
  background: #f8fafc;
  padding: 0.75rem 1rem;
  font-size: 0.8125rem;
  color: #64748b;
  border-bottom: 1px solid #e2e8f0;
}

/* Блоки кода */
.code-block {
  font-family: 'Fira Code', monospace;
  font-size: 0.8125rem;
  line-height: 1.5;
  color: #f8fafc; /* Чёткий контрастный светлый текст */
  background: #1e293b; /* Тёмно-синий/серый фон (Slate 800) */
  margin: 0;
  padding: 1rem;
  white-space: pre-wrap;
  word-break: break-word;
}

/* Таблицы */
.modern-table-wrapper {
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  overflow: hidden;
}
.modern-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.875rem;
}
.modern-table th {
  background: #f8fafc;
  color: #64748b;
  font-weight: 600;
  text-align: left;
  padding: 0.75rem 1rem;
  border-bottom: 1px solid #e2e8f0;
}
.modern-table td {
  padding: 0.75rem 1rem;
  border-bottom: 1px solid #f1f5f9;
  color: #334155;
}
.modern-table tr:last-child td {
  border-bottom: none;
}
.token-cell {
  word-break: break-all;
}
.hash-subtext {
  font-size: 0.75rem;
  color: #94a3b8;
}

/* Иконки в таблицах */
.check-icon {
  color: #10b981;
  font-weight: bold;
}
.dash-icon {
  color: #cbd5e1;
}

/* Метрики */
.metric-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 1rem;
}
.metric-header {
  font-size: 0.875rem;
  font-weight: 600;
  color: #0f172a;
  margin-bottom: 0.75rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid #f1f5f9;
}
.metric-key {
  color: #64748b;
  padding: 0.35rem 0 !important;
}
.metric-val {
  font-weight: 500;
  color: #0f172a;
  text-align: right;
  padding: 0.35rem 0 !important;
}

/* Доп. элементы */
.badge-soft {
  background: #f1f5f9;
  color: #475569;
  padding: 4px 10px;
  border-radius: 6px;
  font-size: 0.75rem;
  font-weight: 500;
}
.sub-heading {
  font-size: 0.9375rem;
  font-weight: 600;
  color: #0f172a;
  margin-bottom: 0.75rem;
}
.text-primary-soft {
  color: #3b82f6; /* Мягкий синий акцент */
}
.empty-state {
  padding: 1.5rem;
  text-align: center;
  color: #94a3b8;
  font-size: 0.875rem;
  background: #1e293b;
  border-bottom-left-radius: 10px;
  border-bottom-right-radius: 10px;
}
.conclusion-box {
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  color: #166534;
  padding: 1rem;
  border-radius: 8px;
  font-size: 0.875rem;
}
.row.g-0 > .col-6.border-end-soft {
  border-right: 1px solid #334155 !important;
  background-color: #1e293b;
}
.graph-print-container {
  height: auto;
  min-height: 250px;
  background-color: #1e293b; /* Тёмный фон вместо старого #fafafa */
  border-bottom-left-radius: 10px;
  border-bottom-right-radius: 10px;
}
.footer-text {
  letter-spacing: 0.02em;
}

/* Dark Mode Overrides (Ідентично до AnalyzerView) */
[data-bs-theme="dark"] .elegant-wrapper { background-color: #212529; color: #adb5bd; }
[data-bs-theme="dark"] .document-page { background-color: #212529; border-color: #495057; }
[data-bs-theme="dark"] .btn-modern-ghost { border-color: #495057; color: #adb5bd; }
[data-bs-theme="dark"] .btn-modern-ghost:hover { background: #343a40; color: #f8f9fa; }
[data-bs-theme="dark"] .report-title,
[data-bs-theme="dark"] .category-title,
[data-bs-theme="dark"] .algo-title,
[data-bs-theme="dark"] .stat-value,
[data-bs-theme="dark"] .metric-val,
[data-bs-theme="dark"] .file-name { color: #f8f9fa !important; }
[data-bs-theme="dark"] .report-meta,
[data-bs-theme="dark"] .stat-desc { color: #adb5bd !important; }
[data-bs-theme="dark"] .brand-badge { background: #343a40; color: #f8f9fa; }
[data-bs-theme="dark"] .border-bottom-soft,
[data-bs-theme="dark"] .border-top-soft,
[data-bs-theme="dark"] .border-end-soft { border-color: #495057 !important; }
[data-bs-theme="dark"] .stat-card,
[data-bs-theme="dark"] .file-card,
[data-bs-theme="dark"] .evidence-card,
[data-bs-theme="dark"] .metric-card { background: #2b3035; border-color: #495057; }
[data-bs-theme="dark"] .category-header { border-bottom-color: #495057; }
[data-bs-theme="dark"] .score-pill { background: #343a40; border-color: #495057; color: #f8f9fa; }
[data-bs-theme="dark"] .algo-section { border-left-color: #495057; }
[data-bs-theme="dark"] .algo-score { color: #adb5bd; }
[data-bs-theme="dark"] .evidence-header { background: #343a40; color: #adb5bd; border-bottom-color: #495057; }
[data-bs-theme="dark"] .modern-table-wrapper { border-color: #495057; }
[data-bs-theme="dark"] .modern-table th { background: #343a40; color: #adb5bd; border-bottom-color: #495057; }
[data-bs-theme="dark"] .modern-table td { border-bottom-color: #495057; color: #f8f9fa; }
[data-bs-theme="dark"] .badge-soft { background: #343a40; color: #f8f9fa; }
[data-bs-theme="dark"] .sub-heading { color: #f8f9fa; }
[data-bs-theme="dark"] .text-primary-soft { color: #6ea8fe; }
[data-bs-theme="dark"] .empty-state { background: #2b3035; color: #adb5bd; }
[data-bs-theme="dark"] .conclusion-box { background: rgba(25, 135, 84, 0.15); border-color: rgba(25, 135, 84, 0.4); color: #75b798; }
[data-bs-theme="dark"] .text-dark { color: #f8f9fa !important; }
[data-bs-theme="dark"] .graph-print-container { background-color: #212529; }
</style>