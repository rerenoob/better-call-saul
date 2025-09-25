import React, { useState } from 'react';
import { useAuth } from '../../hooks/useAuth';
import { LoginRequest } from '../../types/auth';

interface AdminLoginFormProps {
  onSuccess?: () => void;
  onSwitchToUserLogin?: () => void;
}

export const AdminLoginForm: React.FC<AdminLoginFormProps> = ({ onSuccess, onSwitchToUserLogin }) => {
  const [credentials, setCredentials] = useState<LoginRequest>({
    email: '',
    password: '',
  });
  const [isLoading, setIsLoading] = useState(false);
  const { login, error, clearError } = useAuth();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    clearError();

    try {
      await login(credentials);
      onSuccess?.();
    } catch (error) {
      console.error('Admin login error:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setCredentials(prev => ({
      ...prev,
      [name]: value,
    }));
  };

  return (
    <div className="w-full max-w-md mx-auto bg-white rounded-xl shadow-lg p-4 sm:p-6 md:p-8 border border-gray-100">
      <div className="text-center mb-4 sm:mb-6">
        <div className="mx-auto w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mb-3">
          <svg className="h-8 w-8 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
        </div>
        <h2 className="text-xl sm:text-2xl font-bold text-gray-800">
          🔐 Admin Login
        </h2>
        <p className="text-sm text-gray-600 mt-1">
          Access administrative controls and system management
        </p>
      </div>
      
      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-3 py-2 sm:px-4 sm:py-3 rounded mb-3 sm:mb-4 text-xs sm:text-sm">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-3 sm:space-y-4">
        <div>
          <label htmlFor="email" className="block text-xs sm:text-sm font-medium text-gray-700">
            Admin Email
          </label>
          <input
            type="email"
            id="email"
            name="email"
            value={credentials.email}
            onChange={handleChange}
            required
            className="mt-1 block w-full px-4 py-3 text-base border border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-red-500 focus:border-red-500 transition-colors duration-200"
            placeholder="admin@example.com"
            autoComplete="email"
          />
        </div>

        <div>
          <label htmlFor="password" className="block text-xs sm:text-sm font-medium text-gray-700">
            Admin Password
          </label>
          <input
            type="password"
            id="password"
            name="password"
            value={credentials.password}
            onChange={handleChange}
            required
            className="mt-1 block w-full px-4 py-3 text-base border border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-red-500 focus:border-red-500 transition-colors duration-200"
            placeholder="Enter admin password"
            autoComplete="current-password"
          />
        </div>

        <button
          type="submit"
          disabled={isLoading}
          className="w-full bg-red-600 text-white py-3 px-4 rounded-lg hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed text-base font-medium transition-colors duration-200 transform hover:scale-[1.02] active:scale-[0.98]"
        >
          {isLoading ? '🔐 Logging in...' : '⚡ Admin Login'}
        </button>
      </form>

      {onSwitchToUserLogin && (
        <div className="mt-3 sm:mt-4 text-center">
          <p className="text-xs sm:text-sm text-gray-600">
            Regular user?{' '}
            <button
              type="button"
              onClick={onSwitchToUserLogin}
              className="text-blue-600 hover:text-blue-800 font-medium text-xs sm:text-sm"
            >
              User login here
            </button>
          </p>
        </div>
      )}
    </div>
  );
};