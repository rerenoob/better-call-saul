import React, { useState, useEffect } from 'react';
import { adminService, type SystemHealth as SystemHealthType } from '../../services/adminService';

export const SystemHealth: React.FC = () => {
  const [healthData, setHealthData] = useState<SystemHealthType | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchSystemHealth = async () => {
    try {
      setIsLoading(true);
      const data = await adminService.getSystemHealth();
      setHealthData(data);
      setError(null);
    } catch (err) {
      setError('Failed to load system health data');
      console.error('Error fetching system health:', err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchSystemHealth();
    
    // Refresh data every 30 seconds
    const interval = setInterval(fetchSystemHealth, 30000);
    return () => clearInterval(interval);
  }, []);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <div className="flex">
          <div className="flex-shrink-0">
            <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-red-800">Error loading system health</h3>
            <div className="mt-2 text-sm text-red-700">
              <p>{error}</p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (!healthData) {
    return (
      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
        <div className="flex">
          <div className="flex-shrink-0">
            <svg className="h-5 w-5 text-yellow-400" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
            </svg>
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-yellow-800">No system health data available</h3>
          </div>
        </div>
      </div>
    );
  }

  const getDatabaseStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'healthy':
        return 'text-green-600 bg-green-100 dark:text-green-400 dark:bg-green-900';
      case 'unhealthy':
        return 'text-red-600 bg-red-100 dark:text-red-400 dark:bg-red-900';
      default:
        return 'text-yellow-600 bg-yellow-100 dark:text-yellow-400 dark:bg-yellow-900';
    }
  };

  const getMemoryUsageStatus = (usageMB: number) => {
    if (usageMB < 100) return 'text-green-600 bg-green-100 dark:text-green-400 dark:bg-green-900';
    if (usageMB < 500) return 'text-yellow-600 bg-yellow-100 dark:text-yellow-400 dark:bg-yellow-900';
    return 'text-red-600 bg-red-100 dark:text-red-400 dark:bg-red-900';
  };

  const getErrorStatus = (errorCount: number) => {
    if (errorCount === 0) return 'text-green-600 bg-green-100 dark:text-green-400 dark:bg-green-900';
    if (errorCount < 10) return 'text-yellow-600 bg-yellow-100 dark:text-yellow-400 dark:bg-yellow-900';
    return 'text-red-600 bg-red-100 dark:text-red-400 dark:bg-red-900';
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">System Health</h1>
        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
          Monitor system performance and health metrics
        </p>
      </div>

      {/* Health Status Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6">
        {/* Database Status */}
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 sm:p-6 shadow-sm">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <svg className="h-6 w-6 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
              </svg>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600 dark:text-gray-400">Database</p>
              <div className="mt-1">
                <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getDatabaseStatusColor(healthData.database)}`}>
                  {healthData.database}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Memory Usage */}
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 sm:p-6 shadow-sm">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <svg className="h-6 w-6 text-purple-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
              </svg>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600 dark:text-gray-400">Memory Usage</p>
              <div className="mt-1">
                <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getMemoryUsageStatus(healthData.memoryUsageMB)}`}>
                  {healthData.memoryUsageMB} MB
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Uptime */}
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 sm:p-6 shadow-sm">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <svg className="h-6 w-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600 dark:text-gray-400">Uptime</p>
              <p className="text-lg font-semibold text-gray-900 dark:text-white">{healthData.uptime}</p>
            </div>
          </div>
        </div>

        {/* Recent Errors */}
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 sm:p-6 shadow-sm">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <svg className="h-6 w-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.864-.833-2.634 0L4.18 16.5c-.77.833.192 2.5 1.732 2.5z" />
              </svg>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-600 dark:text-gray-400">Recent Errors (24h)</p>
              <div className="mt-1">
                <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getErrorStatus(healthData.recentErrors)}`}>
                  {healthData.recentErrors} errors
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Detailed Information */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow overflow-hidden">
        <div className="px-4 sm:px-6 py-4 border-b border-gray-200 dark:border-gray-700">
          <h2 className="text-lg font-medium text-gray-900 dark:text-white">System Information</h2>
        </div>
        <div className="px-4 sm:px-6 py-4">
          <dl className="grid grid-cols-1 gap-x-4 gap-y-4 sm:gap-y-6 sm:grid-cols-2">
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Database Status</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-white">{healthData.database}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Memory Usage</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-white">{healthData.memoryUsageMB} MB</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">System Uptime</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-white">{healthData.uptime}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Recent Errors</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-white">{healthData.recentErrors} errors in last 24 hours</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Last Updated</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-white">
                {new Date(healthData.timestamp).toLocaleString()}
              </dd>
            </div>
          </dl>
        </div>
      </div>

      {/* Refresh Button */}
      <div className="flex justify-end">
        <button
          onClick={fetchSystemHealth}
          className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
        >
          Refresh Data
        </button>
      </div>
    </div>
  );
};