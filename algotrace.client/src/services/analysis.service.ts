import { reactive } from 'vue';
import api from './api';

export interface FileContent {
  filename: string;
  content: string;
}

export interface AnalysisPayload {
  language: string;
  submissionA: {
    files: FileContent[];
  };
  submissionB: {
    files: FileContent[];
  };
  analysisConfig: {
    methods: string[];
    parameters: {
      ignore_comments: boolean;
      ignore_whitespace: boolean;
    };
  };
}

// Глобальное состояние для хранения результатов анализа
export const analysisState = reactive({
  currentReport: null as Record<string, unknown> | null
});

export const analysisService = {
  async analyze(endpoint: string, payload: AnalysisPayload) {
    // Sending POST request to the specific category endpoint
    return api.post(endpoint, payload);
  },

  async uploadFile(file: File) {
    const formData = new FormData();
    formData.append('file', file);

    return api.post<{ language: string; submission: { files: FileContent[] } }>('/api/submission/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  }
};
