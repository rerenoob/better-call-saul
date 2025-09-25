import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '../contexts/AuthContext';
import { Dashboard } from './Dashboard';

// Mock the case service properly
vi.mock('../services/caseService', async () => {
  const actual = await vi.importActual('../services/caseService');
  return {
    ...actual,
    getCases: vi.fn().mockResolvedValue([]),
    getCaseStatistics: vi.fn().mockResolvedValue({
      totalCases: 0,
      openCases: 0,
      closedCases: 0,
    }),
  };
});

// Create a wrapper component with required providers
const TestWrapper = ({ children }: { children: React.ReactNode }) => (
  <BrowserRouter>
    <AuthProvider>
      {children}
    </AuthProvider>
  </BrowserRouter>
);

describe('Dashboard', () => {
  it('renders dashboard with basic elements', () => {
    render(<Dashboard />, { wrapper: TestWrapper });
    
    expect(screen.getByText(/case dashboard/i)).toBeInTheDocument();
  });
});
