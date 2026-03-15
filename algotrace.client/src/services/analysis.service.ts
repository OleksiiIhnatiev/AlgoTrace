import { reactive } from 'vue';
import api from './api';

export interface FileContent {
  filename: string;
  content: string;
}

export interface AnalysisPayload {
  language: string;
  submission_a: {
    files: FileContent[];
  };
  submission_b: {
    files: FileContent[];
  };
  analysis_config: {
    parameters: {
      ignore_comments: boolean;
      ignore_whitespace: boolean;
    };
    execute_categories: Record<string, string[]>;
  };
}

export interface AnalysisMultiplePayload {
  language: string;
  submission: {
    files: FileContent[];
  };
  compare_with_document_ids: string[];
  analysis_config: {
    categories: {
      category_name: string;
      methods: string[];
    }[];
    parameters: {
      ignore_comments: boolean;
      ignore_whitespace: boolean;
    };
  };
}

export const analysisState = reactive({
  currentReport: null as Record<string, unknown> | null
});

export const analysisService = {
  async analyze(endpoint: string, payload: AnalysisPayload) {
    return api.post(endpoint, payload);
  },

  async analyzeMultiple(payload: AnalysisMultiplePayload) {
    return api.post('/api/analysis/compare-multiple', payload);
  },

  async uploadFile(file: File) {
    const formData = new FormData();
    formData.append('file', file);

    return api.post<{ language: string; submission: { files: FileContent[] } }>('/api/submission/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  }
};
