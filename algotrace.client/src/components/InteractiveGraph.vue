<script setup lang="ts">
import { computed, ref, watch, onMounted, onBeforeUnmount, nextTick } from 'vue';
import cytoscape from 'cytoscape';
import dagre from 'cytoscape-dagre';
import { isDarkMode } from '../composables/useTheme';
import { use } from 'echarts/core';
import { GraphChart, TreeChart } from 'echarts/charts';
import { TooltipComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import VChart from 'vue-echarts';

// Глобальний патч для уникнення willReadFrequently
if (typeof window !== 'undefined' && HTMLCanvasElement) {
  const origGetContext = HTMLCanvasElement.prototype.getContext;
  if (!(origGetContext as unknown as { __patched?: boolean }).__patched) {
    HTMLCanvasElement.prototype.getContext = function(this: HTMLCanvasElement, type: string, attributes?: CanvasRenderingContext2DSettings | undefined) {
      if (type === '2d') {
        attributes = { ...(attributes || {}), willReadFrequently: true };
      }
      return origGetContext.call(this, type, attributes as CanvasRenderingContext2DSettings);
    } as typeof HTMLCanvasElement.prototype.getContext;
    (HTMLCanvasElement.prototype.getContext as unknown as { __patched?: boolean }).__patched = true;
  }
}

cytoscape.use(dagre);

use([GraphChart, TreeChart, TooltipComponent, CanvasRenderer]);

interface GraphNode {
  id: string | number;
  label?: string;
  color?: { background?: string; border?: string; highlight?: { background?: string; border?: string } | string };
  font?: { color?: string };
  borderWidth?: number;
}

interface GraphEdge {
  from: string | number;
  to: string | number;
  label?: string;
  color?: { color?: string; highlight?: string };
  width?: number;
}

interface GraphData {
  nodes: GraphNode[];
  edges: GraphEdge[];
}

const props = defineProps<{
  graphData: GraphData;
  options?: Record<string, unknown>;
  height?: string;
  isGraph?: boolean;
}>();

const isCFG = computed(() => {
  if (props.isGraph !== undefined) return props.isGraph;
  if (!props.graphData?.edges) return false;
  const edges = props.graphData.edges;
  const inDegree = new Map();
  edges.forEach(e => {
      const to = e.to.toString();
      inDegree.set(to, (inDegree.get(to) || 0) + 1);
  });
  return edges.some(e => e.label) || Array.from(inDegree.values()).some(d => d > 1);
});

const getBgColor = (n: GraphNode) => n.color?.background || (isDarkMode.value ? '#2a2a2a' : '#ffffff');
const getBorderColor = (n: GraphNode) => n.color?.border || (isDarkMode.value ? '#495057' : '#ced4da');
const getTextColor = (n: GraphNode) => n.font?.color || (isDarkMode.value ? '#e0e0e0' : '#212529');

const cyContainer = ref<HTMLElement | null>(null);
let cyInstance: cytoscape.Core | null = null;

const initCytoscape = () => {
  if (!isCFG.value || !cyContainer.value || !props.graphData?.nodes?.length) return;

  if (cyInstance) cyInstance.destroy();

  const nodes = props.graphData.nodes.map(n => ({
    data: {
      id: n.id.toString(),
      label: (n.label || '').substring(0, 40),
      bgColor: getBgColor(n),
      borderColor: getBorderColor(n),
      fontColor: getTextColor(n),
      borderWidth: n.borderWidth || 2
    }
  }));

  const edges = props.graphData.edges.map(e => ({
    data: {
      source: e.from.toString(),
      target: e.to.toString(),
      label: e.label || '',
      color: e.color?.color || (isDarkMode.value ? '#666666' : '#adb5bd'),
      width: e.width || 2
    }
  }));

  cyInstance = cytoscape({
    container: cyContainer.value,
    elements: [...nodes, ...edges],
    style: [
      {
        selector: 'node',
        style: {
          'shape': 'round-rectangle',
          'background-color': 'data(bgColor)',
          'border-width': 'data(borderWidth)',
          'border-color': 'data(borderColor)',
          'color': 'data(fontColor)',
          'label': 'data(label)',
          'text-valign': 'center',
          'text-halign': 'center',
          'text-wrap': 'wrap',
          'font-family': 'monospace',
          'font-size': '11px',
          'padding': '10px',
          'width': (n: cytoscape.NodeSingular) => {
            const label = n.data('label') || '';
            const lines = label.split('\n');
            const max = Math.max(...lines.map((l: string) => l.length));
            return Math.max(40, max * 7 + 20);
          },
          'height': (n: cytoscape.NodeSingular) => {
            const label = n.data('label') || '';
            const lines = label.split('\n');
            return Math.max(30, lines.length * 14 + 16);
          }
        }
      },
      {
        selector: 'edge',
        style: {
          'width': 'data(width)',
          'line-color': 'data(color)',
          'target-arrow-color': 'data(color)',
          'target-arrow-shape': 'triangle',
          'curve-style': 'bezier',
          'label': 'data(label)',
          'font-size': '10px',
          'color': isDarkMode.value ? '#aaaaaa' : '#555555',
          'text-background-opacity': 1,
          'text-background-color': isDarkMode.value ? '#212529' : '#ffffff',
          'text-background-padding': '2px',
          'text-background-shape': 'roundrectangle'
        }
      }
    ],
    layout: {
      name: 'dagre',
      rankDir: 'TB',
      nodeSep: 40,
      edgeSep: 40,
      rankSep: 80,
      animate: false
    } as unknown as cytoscape.LayoutOptions
  });
};

watch(() => props.graphData, () => nextTick(() => { if (isCFG.value) initCytoscape(); }), { deep: true });
watch(isDarkMode, () => nextTick(() => { if (isCFG.value) initCytoscape(); }));
onMounted(() => nextTick(() => { if (isCFG.value) initCytoscape(); }));
onBeforeUnmount(() => { if (cyInstance) cyInstance.destroy(); });

const treeChartOption = computed(() => {
  if (isCFG.value || !props.graphData || !props.graphData.nodes || props.graphData.nodes.length === 0) {
    return {};
  }

  const nodes = props.graphData.nodes;
  const edges = props.graphData.edges;
  const isMassive = nodes.length > 300;

    // Рендер Дерева (AST)
    const nodeMap = new Map();
    nodes.forEach(n => {
      nodeMap.set(n.id.toString(), {
        name: n.label,
        value: n.id.toString(),
        label: {
          color: getTextColor(n),
          fontFamily: 'monospace',
          fontSize: 12,
          backgroundColor: getBgColor(n),
          borderColor: getBorderColor(n),
          borderWidth: n.borderWidth || 1,
          padding: [4, 6],
          borderRadius: 4
        },
        children: []
      });
    });

    const inDegree = new Map();
    nodes.forEach(n => inDegree.set(n.id.toString(), 0));
    
    const visitedEdges = new Set();
    edges.forEach(e => {
      const fromId = e.from.toString();
      const toId = e.to.toString();
      const edgeKey = `${fromId}->${toId}`;
      if (visitedEdges.has(edgeKey)) return;
      visitedEdges.add(edgeKey);

      const parent = nodeMap.get(fromId);
      const child = nodeMap.get(toId);
      
      if (parent && child) {
        parent.children.push(child);
        inDegree.set(toId, (inDegree.get(toId) || 0) + 1);
      }
    });

    const roots: Record<string, unknown>[] = [];
    inDegree.forEach((degree, id) => {
      if (degree === 0) roots.push(nodeMap.get(id));
    });

    const treeData = roots.length > 0 ? roots[0] : (nodes.length > 0 ? nodeMap.values().next().value : {});

    return {
      tooltip: { trigger: 'item', triggerOn: 'mousemove', formatter: '{b}' },
      series: [{
        type: 'tree',
        data: [treeData],
        top: '5%',
        left: '10%',
        bottom: '5%',
        right: '10%',
        symbol: 'emptyCircle',
        symbolSize: 1, // Скрываем стандартный кружок, оставляем только стилизованный лейбл-коробку
        initialTreeDepth: isMassive ? 1 : (nodes.length > 50 ? 2 : 4), // Гигантские деревья по умолчанию скрываем до корня
        label: { position: 'top', verticalAlign: 'middle', align: 'center' },
        leaves: { label: { position: 'bottom', verticalAlign: 'middle', align: 'center' } },
        expandAndCollapse: true,
        animationDuration: 500,
        animationDurationUpdate: 500
      }]
    };
});
</script>

<template>
  <div class="w-100 rounded-3 border" :class="isDarkMode ? 'bg-dark' : 'bg-white'" :style="{ height: height || '400px' }">
     <div v-if="isCFG" ref="cyContainer" class="w-100 h-100 d-block" style="min-height: inherit;"></div>
     <v-chart v-else class="w-100 h-100" style="width: 100%; height: 100%; display: block;" :option="treeChartOption" autoresize />
  </div>
</template>
