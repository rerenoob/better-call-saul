import { apiClient } from './apiClient';
import { Case, CaseStatistics, CaseFilters } from '../types/case';

export interface CaseWithDocuments {
  case: Case;
  documents: Array<{ id: string; name: string }>;
  analyses: Array<{ id: string; status: string }>;
}

export const caseService = {
  getCases: async (filters?: CaseFilters): Promise<Case[]> => {
    const params = new URLSearchParams();
    if (filters?.status) params.append('status', filters.status);
    if (filters?.assignedTo) params.append('assignedTo', filters.assignedTo);
    if (filters?.search) params.append('search', filters.search);

    const response = await apiClient.get<Case[]>(`/api/case?${params.toString()}`);
    return response.data;
  },

  getCase: async (id: string): Promise<CaseWithDocuments> => {
    const response = await apiClient.get<CaseWithDocuments>(`/api/case/${id}`);
    return response.data;
  },

  createCase: async (caseData: { title: string; description?: string }): Promise<Case> => {
    const response = await apiClient.post<Case>('/api/case', caseData);
    return response.data;
  },

  updateCase: async (id: string, caseData: Partial<Case>): Promise<Case> => {
    const response = await apiClient.put<Case>(`/api/case/${id}`, caseData);
    return response.data;
  },

  deleteCase: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/case/${id}`);
  },

  getStatistics: async (): Promise<CaseStatistics> => {
    const response = await apiClient.get<CaseStatistics>('/api/case/statistics');
    return response.data;
  },

  getRecentCases: async (limit: number = 10): Promise<Case[]> => {
    const response = await apiClient.get<Case[]>(`/api/case/recent?limit=${limit}`);
    return response.data;
  },

  chatWithAI: async (
    caseId: string,
    message: string
  ): Promise<{ success: boolean; generatedText?: string; errorMessage?: string }> => {
    const response = await apiClient.post(`/api/case/${caseId}/chat`, { message });
    return response.data;
  },

  analyzeCase: async (caseId: string, documentId: string): Promise<{ success: boolean; analysisId?: string; message?: string }> => {
    const response = await apiClient.post(`/api/caseanalysis/analyze/${caseId}`, { 
      documentId,
      analysisType: 'comprehensive',
      includeViabilityAssessment: true
    });
    return response.data;
  },
};
