import { apiClient } from './apiClient';

export interface DashboardMetrics {
  totalUsers: number;
  activeUsers: number;
  casesAnalyzed24h: number;
  avgAnalysisTime: number;
  activeIncidents: number;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  barNumber?: string;
  lawFirm?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface UserDetails extends User {
  casesCount: number;
  lastActivity?: string;
}

export interface UsersResponse {
  users: User[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface SystemHealth {
  database: string;
  memoryUsageMB: number;
  uptime: string;
  recentErrors: number;
  timestamp: string;
}

export interface AuditLog {
  id: string;
  message: string;
  level: string;
  action: string;
  createdAt: string;
  user?: {
    fullName: string;
    email: string;
  };
  ipAddress?: string;
  userAgent?: string;
}

export interface AuditLogsResponse {
  logs: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CaseStatistics {
  totalCases: number;
  casesByStatus: Array<{ status: string; count: number }>;
  casesByDay: Array<{ date: string; count: number }>;
}

export interface Case {
  id: string;
  caseNumber: string;
  title: string;
  description?: string;
  status: string;
  userId: string;
  userName: string;
  userEmail: string;
  successProbability?: number;
  hearingDate?: string;
  createdAt: string;
  updatedAt: string;
  documentCount: number;
  analysisStatus?: string;
}

export interface CaseDetails extends Case {
  documents: Array<{
    id: string;
    fileName: string;
    fileSize: number;
    fileType: string;
    status: string;
    uploadedAt: string;
    extractedText?: string;
  }>;
  analyses: Array<{
    id: string;
    status: string;
    viabilityScore?: number;
    analysisText?: string;
    createdAt: string;
    updatedAt: string;
  }>;
}

export interface AnalyzeCaseRequest {
  documentId: string;
  analysisType?: string;
  includeViabilityAssessment?: boolean;
  includeSimilarCases?: boolean;
}

export interface CaseAnalysisResponse {
  analysisId: string;
  caseId: string;
  status: string;
  viabilityScore?: number;
  confidenceScore?: number;
  summary?: string;
  keyIssues?: string[];
  potentialDefenses?: string[];
  evidenceGaps?: string[];
  recommendedActions?: string[];
  similarCases?: string[];
  createdAt: string;
  completedAt?: string;
}

export interface ViabilityAssessmentRequest {
  caseFacts: string;
  charges: string[];
  evidence: string[];
}

export interface ViabilityAssessmentResponse {
  caseId: string;
  viabilityScore: number;
  confidenceLevel: string;
  reasoning: string;
  strengthFactors: string[];
  weaknessFactors: string[];
  recommendedStrategy: string;
  assessedAt: string;
}

export interface CasesResponse {
  cases: Case[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface RegistrationCode {
  id: string;
  code: string;
  createdBy: string;
  isUsed: boolean;
  usedByUserId?: string;
  usedAt?: string;
  expiresAt: string;
  createdAt: string;
  updatedAt?: string;
  notes?: string;
  usedByUserName?: string;
  isValid: boolean;
}

export interface RegistrationCodesResponse {
  codes: RegistrationCode[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface RegistrationCodeStats {
  total: number;
  active: number;
  used: number;
  expired: number;
}

export interface GenerateCodesRequest {
  count: number;
  expireDays: number;
  createdBy?: string;
  notes?: string;
}

export const adminService = {
  async getDashboardMetrics(): Promise<DashboardMetrics> {
    const response = await apiClient.get('/api/admin/dashboard/metrics');
    return response.data;
  },

  async getUsers(page: number = 1, pageSize: number = 20): Promise<UsersResponse> {
    const response = await apiClient.get('/api/admin/users', {
      params: { page, pageSize },
    });
    return response.data;
  },

  async getUser(id: string): Promise<UserDetails> {
    const response = await apiClient.get(`/api/admin/users/${id}`);
    return response.data;
  },

  async updateUserStatus(id: string, isActive: boolean): Promise<void> {
    await apiClient.put(`/api/admin/users/${id}/status`, { isActive });
  },

  async getSystemHealth(): Promise<SystemHealth> {
    const response = await apiClient.get('/api/admin/system/health');
    return response.data;
  },

  async getAuditLogs(page: number = 1, pageSize: number = 50): Promise<AuditLogsResponse> {
    const response = await apiClient.get('/api/admin/audit-logs', {
      params: { page, pageSize },
    });
    return response.data;
  },

  async getCaseStatistics(): Promise<CaseStatistics> {
    const response = await apiClient.get('/api/admin/cases/stats');
    return response.data;
  },

  async getRegistrationCodes(
    page: number = 1,
    pageSize: number = 50
  ): Promise<RegistrationCodesResponse> {
    const response = await apiClient.get('/api/admin/registration-codes', {
      params: { page, pageSize },
    });
    return response.data;
  },

  async getRegistrationCodeStats(): Promise<RegistrationCodeStats> {
    const response = await apiClient.get('/api/admin/registration-codes/stats');
    return response.data;
  },

  async generateRegistrationCodes(
    request: GenerateCodesRequest
  ): Promise<{ message: string; codes: string[] }> {
    const response = await apiClient.post('/api/admin/registration-codes/generate', request);
    return response.data;
  },

  async deleteRegistrationCode(id: string): Promise<void> {
    await apiClient.delete(`/api/admin/registration-codes/${id}`);
  },

  async cleanupExpiredCodes(): Promise<{ message: string }> {
    const response = await apiClient.post('/api/admin/registration-codes/cleanup');
    return response.data;
  },

  async getCases(
    page: number = 1,
    pageSize: number = 20,
    search?: string,
    status?: string
  ): Promise<CasesResponse> {
    const params: { page: number; pageSize: number; search?: string; status?: string } = { page, pageSize };
    if (search) params.search = search;
    if (status) params.status = status;
    
    const response = await apiClient.get('/api/admin/cases', { params });
    return response.data;
  },

  async getCase(id: string): Promise<CaseDetails> {
    const response = await apiClient.get(`/api/admin/cases/${id}`);
    return response.data;
  },

  async updateCaseStatus(id: string, status: string): Promise<void> {
    await apiClient.put(`/api/admin/cases/${id}/status`, { status });
  },

  async updateCase(id: string, updates: Partial<CaseDetails>): Promise<CaseDetails> {
    const response = await apiClient.put(`/api/admin/cases/${id}`, updates);
    return response.data;
  },

  async deleteCase(id: string): Promise<void> {
    await apiClient.delete(`/api/admin/cases/${id}`);
  },

  // AI Analysis Methods
  async analyzeCase(caseId: string, request: AnalyzeCaseRequest): Promise<{ success: boolean; analysisId: string; message: string }> {
    const response = await apiClient.post(`/api/case-analysis/analyze/${caseId}`, request);
    return response.data;
  },

  async getAnalysis(analysisId: string): Promise<CaseAnalysisResponse> {
    const response = await apiClient.get(`/api/case-analysis/analysis/${analysisId}`);
    return response.data;
  },

  async getCaseAnalyses(caseId: string): Promise<CaseAnalysisResponse[]> {
    const response = await apiClient.get(`/api/case-analysis/case/${caseId}/analyses`);
    return response.data;
  },

  async assessViability(caseId: string, request: ViabilityAssessmentRequest): Promise<ViabilityAssessmentResponse> {
    const response = await apiClient.post(`/api/case-analysis/viability/${caseId}`, request);
    return response.data;
  },
};
