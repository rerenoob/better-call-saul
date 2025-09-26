import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AppLayout } from './layout/AppLayout';
import { caseService } from '../services/caseService';

export const CaseCreate: React.FC = () => {
  const navigate = useNavigate();
  const [caseName, setCaseName] = useState('');
  const [clientName, setClientName] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState('');

  const handleCreateCase = async () => {
    if (!caseName.trim()) {
      setError('Please enter a case name');
      return;
    }

    if (!clientName.trim()) {
      setError('Please enter a client name');
      return;
    }

    setError('');
    setIsCreating(true);

    try {
      const newCase = await caseService.createCase({
        title: caseName.trim(),
        description: `Client: ${clientName.trim()}`,
      });

      // Navigate to file upload page for this case
      navigate(`/cases/${newCase.id}/upload`);
    } catch (error: unknown) {
      console.error('Error creating case:', error);
      setError(
        error instanceof Error && 'response' in error && error.response && typeof error.response === 'object' && 'data' in error.response && error.response.data && typeof error.response.data === 'object' && 'message' in error.response.data
          ? (error.response.data as { message: string }).message
          : 'Failed to create case. Please try again.'
      );
      setIsCreating(false);
    }
  };

  const canCreate = caseName.trim() && clientName.trim() && !isCreating;

  return (
    <AppLayout>
      <div className="p-6 max-w-2xl mx-auto">
        <div className="bg-white rounded-lg shadow-lg p-6">
          <h1 className="text-2xl font-bold text-gray-900 mb-6">Create New Case</h1>
          
          <p className="text-gray-600 mb-6">
            First, create your case. Then you'll be able to upload documents and start the AI analysis.
          </p>

          {/* Case Information Form */}
          <div className="space-y-6 mb-8">
            <div>
              <label htmlFor="caseName" className="block text-sm font-medium text-gray-700 mb-2">
                Case Name *
              </label>
              <input
                id="caseName"
                type="text"
                value={caseName}
                onChange={e => setCaseName(e.target.value)}
                placeholder="e.g., State v. Anderson"
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                disabled={isCreating}
              />
            </div>

            <div>
              <label htmlFor="clientName" className="block text-sm font-medium text-gray-700 mb-2">
                Client Name *
              </label>
              <input
                id="clientName"
                type="text"
                value={clientName}
                onChange={e => setClientName(e.target.value)}
                placeholder="e.g., David Anderson"
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                disabled={isCreating}
              />
            </div>
          </div>

          {/* Error Message */}
          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-md">
              <div className="flex">
                <svg className="w-5 h-5 text-red-400 mr-2" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fillRule="evenodd"
                    d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                    clipRule="evenodd"
                  />
                </svg>
                <p className="text-sm text-red-800">{error}</p>
              </div>
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex justify-end space-x-4">
            <button
              onClick={() => navigate('/dashboard')}
              className="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500"
              disabled={isCreating}
            >
              Cancel
            </button>

            <button
              onClick={handleCreateCase}
              disabled={!canCreate}
              className={`px-6 py-2 text-white rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 flex items-center ${
                canCreate ? 'bg-blue-600 hover:bg-blue-700' : 'bg-gray-400 cursor-not-allowed'
              }`}
            >
              {isCreating ? (
                <>
                  <svg
                    className="animate-spin -ml-1 mr-3 h-5 w-5 text-white"
                    fill="none"
                    viewBox="0 0 24 24"
                  >
                    <circle
                      className="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      strokeWidth="4"
                    />
                    <path
                      className="opacity-75"
                      fill="currentColor"
                      d="m4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                    />
                  </svg>
                  Creating Case...
                </>
              ) : (
                <>
                  <svg className="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M13 6a3 3 0 11-6 0 3 3 0 016 0zM18 8a2 2 0 11-4 0 2 2 0 014 0zM14 15a4 4 0 00-8 0v3h8v-3z" />
                  </svg>
                  Create Case & Upload Files
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </AppLayout>
  );
};