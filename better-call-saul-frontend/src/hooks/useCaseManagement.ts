import { useState, useEffect, useCallback } from 'react';
import { adminService, type Case, type CaseDetails, type CasesResponse, type CaseStatistics, type AnalyzeCaseRequest, type CaseAnalysisResponse, type ViabilityAssessmentRequest, type ViabilityAssessmentResponse } from '../services/adminService';

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
  isAnalyzing: boolean;
  analysisProgress: number;
  currentAnalysis: CaseAnalysisResponse | null;
  analysisError: string | null;
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
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [analysisProgress, setAnalysisProgress] = useState(0);
  const [currentAnalysis, setCurrentAnalysis] = useState<CaseAnalysisResponse | null>(null);
  const [analysisError, setAnalysisError] = useState<string | null>(null);

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

  const updateCase = async (id: string, updates: Partial<CaseDetails>) => {
    try {
      const updatedCase = await adminService.updateCase(id, updates);
      // Refresh the cases list
      await fetchCases();
      // If the updated case is currently selected, update it
      if (selectedCase?.id === id) {
        setSelectedCase(updatedCase);
      }
      return updatedCase;
    } catch (err) {
      setError('Failed to update case');
      console.error('Error updating case:', err);
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

  const analyzeCase = async (caseId: string, request: AnalyzeCaseRequest) => {
    try {
      setIsAnalyzing(true);
      setAnalysisProgress(0);
      setAnalysisError(null);
      
      // Start analysis
      const response = await adminService.analyzeCase(caseId, request);
      
      if (response.success) {
        // Poll for analysis progress
        const pollAnalysis = async (analysisId: string) => {
          let attempts = 0;
          const maxAttempts = 60; // 5 minutes max
          
          while (attempts < maxAttempts) {
            try {
              const analysis = await adminService.getAnalysis(analysisId);
              
              if (analysis.status === 'Completed') {
                setCurrentAnalysis(analysis);
                setAnalysisProgress(100);
                setIsAnalyzing(false);
                
                // Refresh case details to show new analysis
                await fetchCaseDetails(caseId);
                return;
              } else if (analysis.status === 'Failed') {
                setAnalysisError('Analysis failed');
                setIsAnalyzing(false);
                return;
              }
              
              // Update progress (simulate progress since we don't have real progress updates)
              setAnalysisProgress(Math.min(90, attempts * 2));
              
              // Wait before next poll
              await new Promise(resolve => setTimeout(resolve, 5000));
              attempts++;
            } catch (err) {
              console.error('Error polling analysis:', err);
              attempts++;
            }
          }
          
          // Timeout
          setAnalysisError('Analysis timed out');
          setIsAnalyzing(false);
        };
        
        await pollAnalysis(response.analysisId);
      } else {
        setAnalysisError(response.message || 'Failed to start analysis');
        setIsAnalyzing(false);
      }
    } catch (err) {
      setAnalysisError('Failed to analyze case');
      console.error('Error analyzing case:', err);
      setIsAnalyzing(false);
    }
  };

  const assessViability = async (caseId: string, request: ViabilityAssessmentRequest): Promise<ViabilityAssessmentResponse> => {
    try {
      setAnalysisError(null);
      return await adminService.assessViability(caseId, request);
    } catch (err) {
      setAnalysisError('Failed to assess viability');
      console.error('Error assessing viability:', err);
      throw err;
    }
  };

  const getCaseAnalyses = async (caseId: string): Promise<CaseAnalysisResponse[]> => {
    try {
      return await adminService.getCaseAnalyses(caseId);
    } catch (err) {
      console.error('Error getting case analyses:', err);
      return [];
    }
  };

  const clearAnalysis = () => {
    setCurrentAnalysis(null);
    setAnalysisError(null);
    setAnalysisProgress(0);
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
    isAnalyzing,
    analysisProgress,
    currentAnalysis,
    analysisError,
    fetchCases,
    fetchCaseDetails,
    fetchStatistics,
    updateCaseStatus,
    updateCase,
    deleteCase,
    updateFilters,
    goToPage,
    analyzeCase,
    assessViability,
    getCaseAnalyses,
    clearAnalysis,
    setSelectedCase,
  };
};