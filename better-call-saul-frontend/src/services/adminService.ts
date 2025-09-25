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
    const response = await apiClient.get('/admin/dashboard/metrics');
    return response.data;
  },

  async getUsers(page: number = 1, pageSize: number = 20): Promise<UsersResponse> {
    const response = await apiClient.get('/admin/users', {
      params: { page, pageSize }
    });
    return response.data;
  },

  async getUser(id: string): Promise<UserDetails> {
    const response = await apiClient.get(`/admin/users/${id}`);
    return response.data;
  },

  async updateUserStatus(id: string, isActive: boolean): Promise<void> {
    await apiClient.put(`/admin/users/${id}/status`, { isActive });
  },

  async getSystemHealth(): Promise<SystemHealth> {
    const response = await apiClient.get('/admin/system/health');
    return response.data;
  },

  async getAuditLogs(page: number = 1, pageSize: number = 50): Promise<AuditLogsResponse> {
    const response = await apiClient.get('/admin/audit-logs', {
      params: { page, pageSize }
    });
    return response.data;
  },

  async getCaseStatistics(): Promise<CaseStatistics> {
    const response = await apiClient.get('/admin/cases/stats');
    return response.data;
  },

  async getRegistrationCodes(page: number = 1, pageSize: number = 50): Promise<RegistrationCodesResponse> {
    const response = await apiClient.get('/admin/registration-codes', {
      params: { page, pageSize }
    });
    return response.data;
  },

  async getRegistrationCodeStats(): Promise<RegistrationCodeStats> {
    const response = await apiClient.get('/admin/registration-codes/stats');
    return response.data;
  },

  async generateRegistrationCodes(request: GenerateCodesRequest): Promise<{ message: string; codes: string[] }> {
    const response = await apiClient.post('/admin/registration-codes/generate', request);
    return response.data;
  },

  async deleteRegistrationCode(id: string): Promise<void> {
    await apiClient.delete(`/admin/registration-codes/${id}`);
  },

  async cleanupExpiredCodes(): Promise<{ message: string }> {
    const response = await apiClient.post('/admin/registration-codes/cleanup');
    return response.data;
  }
};