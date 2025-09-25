import React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '../../contexts/AuthContext';
import { LoginForm } from './LoginForm';

// Create a wrapper component with required providers
const TestWrapper = ({ children }: { children: React.ReactNode }) => (
  <BrowserRouter>
    <AuthProvider>
      {children}
    </AuthProvider>
  </BrowserRouter>
);

describe('LoginForm', () => {
  const mockOnSuccess = vi.fn();
  const mockOnError = vi.fn();

  it('renders login form with basic elements', () => {
    render(<LoginForm onSuccess={mockOnSuccess} onError={mockOnError} />, { wrapper: TestWrapper });

    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /login/i })).toBeInTheDocument();
  });
});
