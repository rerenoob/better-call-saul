import { useState, useEffect, useCallback } from 'react';
import { adminService, type Case, type CaseDetails, type CasesResponse, type CaseStatistics } from '../services/adminService';

export interface CaseManagementData {
  cases: Case[];
  selectedCase: CaseDetails | null;
  statistics: CaseStatistics;
  isLoading: boolean;
  error: string | null;
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export interface CaseFilters {
  search?: string;
  status?: string;
}

export const useCaseManagement = (
  initialPage: number = 1,
  initialPageSize: number = 20,
  initialFilters: CaseFilters = {}
) => {
  const [cases, setCases] = useState<Case[]>([]);
  const [selectedCase, setSelectedCase] = useState<CaseDetails | null>(null);
  const [statistics, setStatistics] = useState<CaseStatistics>({
    totalCases: 0,
    casesByStatus: [],
    casesByDay: [],
  });
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pagination, setPagination] = useState({
    page: initialPage,
    pageSize: initialPageSize,
    totalCount: 0,
    totalPages: 0,
  });
  const [filters, setFilters] = useState<CaseFilters>(initialFilters);

  const fetchCases = useCallback(async (page: number = pagination.page, pageSize: number = pagination.pageSize) => {
    try {
      setIsLoading(true);
      const response: CasesResponse = await adminService.getCases(
        page,
        pageSize,
        filters.search,
        filters.status
      );
      
      setCases(response.cases);
      setPagination({
        page: response.page,
        pageSize: response.pageSize,
        totalCount: response.totalCount,
        totalPages: response.totalPages,
      });
      setError(null);
    } catch (err) {
      setError('Failed to load cases');
      console.error('Error fetching cases:', err);
    } finally {
      setIsLoading(false);
    }
  }, [pagination.page, pagination.pageSize, filters.search, filters.status]);

  const fetchCaseDetails = async (id: string) => {
    try {
      setIsLoading(true);
      const caseDetails = await adminService.getCase(id);
      setSelectedCase(caseDetails);
      setError(null);
    } catch (err) {
      setError('Failed to load case details');
      console.error('Error fetching case details:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const fetchStatistics = useCallback(async () => {
    try {
      const stats = await adminService.getCaseStatistics();
      setStatistics(stats);
    } catch (err) {
      console.error('Error fetching case statistics:', err);
    }
  }, []);

  const updateCaseStatus = async (id: string, status: string) => {
    try {
      await adminService.updateCaseStatus(id, status);
      // Refresh the cases list
      await fetchCases();
      // If the updated case is currently selected, refresh its details
      if (selectedCase?.id === id) {
        await fetchCaseDetails(id);
      }
    } catch (err) {
      setError('Failed to update case status');
      console.error('Error updating case status:', err);
      throw err;
    }
  };

  const deleteCase = async (id: string) => {
    try {
      await adminService.deleteCase(id);
      // Refresh the cases list
      await fetchCases();
      // Clear selected case if it was deleted
      if (selectedCase?.id === id) {
        setSelectedCase(null);
      }
    } catch (err) {
      setError('Failed to delete case');
      console.error('Error deleting case:', err);
      throw err;
    }
  };

  const updateFilters = (newFilters: CaseFilters) => {
    setFilters(newFilters);
    // Reset to first page when filters change
    setPagination(prev => ({ ...prev, page: 1 }));
  };

  const goToPage = (page: number) => {
    setPagination(prev => ({ ...prev, page }));
  };

  useEffect(() => {
    fetchCases();
    fetchStatistics();
  }, [fetchCases, fetchStatistics]);

  return {
    cases,
    selectedCase,
    statistics,
    isLoading,
    error,
    pagination,
    filters,
    fetchCases,
    fetchCaseDetails,
    fetchStatistics,
    updateCaseStatus,
    deleteCase,
    updateFilters,
    goToPage,
    setSelectedCase,
  };
};