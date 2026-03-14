<script setup lang="ts">
import { ref, onMounted, watch, onBeforeUnmount } from 'vue';
import { Network, type Node, type Edge, type Options } from 'vis-network';

interface GraphData {
  nodes: Node[];
  edges: Edge[];
}

const props = defineProps<{
  graphData: GraphData;
  options?: Options;
}>();

const container = ref<HTMLElement | null>(null);
let network: Network | null = null;

const initGraph = () => {
  if (container.value && props.graphData?.nodes?.length) {
    if (network) network.destroy();

    const defaultOptions: Options = {
      autoResize: true,
      nodes: {
        shape: 'box',
        margin: { top: 12, right: 12, bottom: 12, left: 12 },
        font: { color: '#212529', face: 'monospace', size: 14 },
        color: { background: '#ffffff', border: '#ced4da', highlight: { background: '#e9ecef', border: '#0d6efd' } },
        borderWidth: 2,
        shadow: true
      },
      edges: {
        arrows: 'to',
        color: { color: '#adb5bd', highlight: '#0d6efd' },
        smooth: { enabled: true, type: 'cubicBezier', roundness: 0.5 },
        width: 2
      },
      physics: {
        enabled: true,
        hierarchicalRepulsion: { nodeDistance: 150 }
      },
      layout: {
        hierarchical: { enabled: true, direction: 'UD', sortMethod: 'directed', levelSeparation: 100 }
      }
    };

    network = new Network(container.value, props.graphData, props.options || defaultOptions);
  }
};

onMounted(initGraph);
watch(() => props.graphData, initGraph, { deep: true });
onBeforeUnmount(() => { if (network) network.destroy(); });
</script>
<template>
  <div ref="container" class="w-100 bg-white rounded-3 border" style="height: 400px;"></div>
</template>
