import { apiClient } from './apiClient';
import { Case, CaseStatistics, CaseFilters } from '../types/case';

export const caseService = {
  getCases: async (filters?: CaseFilters): Promise<Case[]> => {
    const params = new URLSearchParams();
    if (filters?.status) params.append('status', filters.status);
    if (filters?.assignedTo) params.append('assignedTo', filters.assignedTo);
    if (filters?.search) params.append('search', filters.search);

    const response = await apiClient.get<Case[]>(`/api/case?${params.toString()}`);
    return response.data;
  },

  getCase: async (id: string): Promise<Case> => {
    const response = await apiClient.get<Case>(`/api/case/${id}`);
    return response.data;
  },

  createCase: async (caseData: Omit<Case, 'id' | 'createdAt' | 'updatedAt'>): Promise<Case> => {
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
};
