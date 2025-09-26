import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { CaseManagement } from './CaseManagement';

// Mock the useCaseManagement hook
vi.mock('../../hooks/useCaseManagement', () => ({
  useCaseManagement: () => ({
    cases: [
      {
        id: '1',
        caseNumber: 'CASE-20240925-ABC123',
        title: 'Test Case',
        description: 'Test description',
        status: 'new',
        userId: 'user1',
        userName: 'John Doe',
        userEmail: 'john@example.com',
        successProbability: 0.75,
        hearingDate: '2024-10-01',
        createdAt: '2024-09-25T10:00:00Z',
        updatedAt: '2024-09-25T10:00:00Z',
        documentCount: 3,
        analysisStatus: 'completed'
      }
    ],
    selectedCase: null,
    statistics: {
      totalCases: 1,
      casesByStatus: [{ status: 'new', count: 1 }],
      casesByDay: []
    },
    isLoading: false,
    error: null,
    pagination: {
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1
    },
    filters: {},
    isAnalyzing: false,
    analysisProgress: 0,
    currentAnalysis: null,
    analysisError: null,
    fetchCaseDetails: vi.fn(),
    updateCase: vi.fn(),
    deleteCase: vi.fn(),
    updateFilters: vi.fn(),
    goToPage: vi.fn(),
    analyzeCase: vi.fn(),
    assessViability: vi.fn(),
    getCaseAnalyses: vi.fn(),
    clearAnalysis: vi.fn()
  })
}));

describe('CaseManagement', () => {
  it('renders case management page with basic elements', () => {
    render(<CaseManagement />);
    
    expect(screen.getByText('Case Management')).toBeInTheDocument();
    expect(screen.getByText('View and manage all uploaded case files')).toBeInTheDocument();
    expect(screen.getByText('Total Cases')).toBeInTheDocument();
    
    // Check for the specific total cases count (the first one in the statistics card)
    const totalCasesElements = screen.getAllByText('1');
    expect(totalCasesElements.length).toBeGreaterThan(0);
  });

  it('displays cases in the table', () => {
    render(<CaseManagement />);
    
    expect(screen.getByText('Test Case')).toBeInTheDocument();
    expect(screen.getByText('CASE-20240925-ABC123')).toBeInTheDocument();
    expect(screen.getByText('John Doe')).toBeInTheDocument();
    expect(screen.getByText('john@example.com')).toBeInTheDocument();
    
    // Use getAllByText for elements that appear multiple times
    const statusElements = screen.getAllByText('new');
    expect(statusElements.length).toBeGreaterThan(0);
    
    expect(screen.getByText('3 files')).toBeInTheDocument();
  });

  it('has action buttons for each case', () => {
    render(<CaseManagement />);
    
    // Check that action buttons are present
    const viewButtons = document.querySelectorAll('[title="View Case Details"]');
    const editButtons = document.querySelectorAll('[title="Edit Case"]');
    const deleteButtons = document.querySelectorAll('[title="Delete Case"]');
    
    expect(viewButtons.length).toBe(1);
    expect(editButtons.length).toBe(1);
    expect(deleteButtons.length).toBe(1);
  });

  it('has search and filter functionality', () => {
    render(<CaseManagement />);
    
    expect(screen.getByPlaceholderText('Search by case number, title, or user...')).toBeInTheDocument();
    expect(screen.getByLabelText('Status Filter')).toBeInTheDocument();
    expect(screen.getByText('Clear Filters')).toBeInTheDocument();
  });
});