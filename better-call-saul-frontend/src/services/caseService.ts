import { apiClient } from './apiClient';
import { Case, CaseStatistics, CaseFilters } from '../types/case';

export const caseService = {
  getCases: async (filters?: CaseFilters): Promise<Case[]> => {
    const params = new URLSearchParams();
    if (filters?.status) params.append('status', filters.status);
    if (filters?.assignedTo) params.append('assignedTo', filters.assignedTo);
    if (filters?.search) params.append('search', filters.search);

    const response = await apiClient.get<Case[]>(`/cases?${params.toString()}`);
    return response.data;
  },

  getCase: async (id: string): Promise<Case> => {
    const response = await apiClient.get<Case>(`/cases/${id}`);
    return response.data;
  },

  createCase: async (caseData: Omit<Case, 'id' | 'createdAt' | 'updatedAt'>): Promise<Case> => {
    const response = await apiClient.post<Case>('/cases', caseData);
    return response.data;
  },

  updateCase: async (id: string, caseData: Partial<Case>): Promise<Case> => {
    const response = await apiClient.put<Case>(`/cases/${id}`, caseData);
    return response.data;
  },

  deleteCase: async (id: string): Promise<void> => {
    await apiClient.delete(`/cases/${id}`);
  },

  getStatistics: async (): Promise<CaseStatistics> => {
    const response = await apiClient.get<CaseStatistics>('/cases/statistics');
    return response.data;
  },

  getRecentCases: async (limit: number = 10): Promise<Case[]> => {
    const response = await apiClient.get<Case[]>(`/cases/recent?limit=${limit}`);
    return response.data;
  },

  chatWithAI: async (caseId: string, message: string): Promise<{ success: boolean; generatedText?: string; errorMessage?: string }> => {
    const response = await apiClient.post(`/cases/${caseId}/chat`, { message });
    return response.data;
  },
};