import React, { useState } from 'react';
import { useCaseManagement } from '../../hooks/useCaseManagement';
import { CaseDetails } from '../../services/adminService';

export const CaseManagement: React.FC = () => {
  const {
    cases,
    selectedCase,
    statistics,
    isLoading,
    error,
    pagination,
    filters,
    fetchCaseDetails,
    updateCase,
    deleteCase,
    updateFilters,
    goToPage,
  } = useCaseManagement();

  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedCaseId, setSelectedCaseId] = useState<string | null>(null);
  const [editingCase, setEditingCase] = useState<CaseDetails | null>(null);

  const handleSearch = (searchTerm: string) => {
    updateFilters({ ...filters, search: searchTerm });
  };

  const handleStatusFilter = (status: string) => {
    updateFilters({ ...filters, status: status || undefined });
  };

  const handleViewCase = async (caseId: string) => {
    setSelectedCaseId(caseId);
    await fetchCaseDetails(caseId);
    setIsDetailModalOpen(true);
  };

  const handleEditCase = async (caseId: string) => {
    setSelectedCaseId(caseId);
    await fetchCaseDetails(caseId);
    setEditingCase(selectedCase);
    setIsEditModalOpen(true);
  };

  const handleDeleteCase = (caseId: string) => {
    setSelectedCaseId(caseId);
    setIsDeleteModalOpen(true);
  };

  const confirmDeleteCase = async () => {
    if (selectedCaseId) {
      try {
        await deleteCase(selectedCaseId);
        setIsDeleteModalOpen(false);
        setSelectedCaseId(null);
      } catch (err) {
        console.error('Error deleting case:', err);
      }
    }
  };



  if (isLoading && cases.length === 0) {
    return (
      <div className="flex items-center justify-center min-h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error && cases.length === 0) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-4">
        <div className="flex">
          <div className="flex-shrink-0">
            <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                clipRule="evenodd"
              />
            </svg>
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-red-800">Error loading cases</h3>
            <div className="mt-2 text-sm text-red-700">
              <p>{error}</p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Case Management</h1>
        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
          View and manage all uploaded case files
        </p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <svg className="h-6 w-6 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                />
              </svg>
            </div>
            <div className="ml-4">
              <p className="text-sm font-medium text-gray-500 dark:text-gray-400">Total Cases</p>
              <p className="text-2xl font-semibold text-gray-900 dark:text-white">
                {statistics.totalCases}
              </p>
            </div>
          </div>
        </div>

        {statistics.casesByStatus.map((statusItem) => (
          <div key={statusItem.status} className="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <div className={`h-6 w-6 rounded-full bg-gray-200`}></div>
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-500 dark:text-gray-400 capitalize">
                  {statusItem.status}
                </p>
                <p className="text-2xl font-semibold text-gray-900 dark:text-white">
                  {statusItem.count}
                </p>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="bg-white dark:bg-gray-800 rounded-lg shadow">
        <div className="px-4 py-5 sm:p-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label htmlFor="search" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                Search Cases
              </label>
              <input
                type="text"
                id="search"
                placeholder="Search by case number, title, or user..."
                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white sm:text-sm"
                value={filters.search || ''}
                onChange={(e) => handleSearch(e.target.value)}
              />
            </div>

            <div>
              <label htmlFor="status" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                Status Filter
              </label>
              <select
                id="status"
                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white sm:text-sm"
                value={filters.status || ''}
                onChange={(e) => handleStatusFilter(e.target.value)}
              >
                <option value="">All Statuses</option>
                <option value="new">New</option>
                <option value="processing">Processing</option>
                <option value="completed">Completed</option>
                <option value="failed">Failed</option>
              </select>
            </div>

            <div className="flex items-end">
              <button
                type="button"
                className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                onClick={() => updateFilters({})}
              >
                Clear Filters
              </button>
            </div>
          </div>
        </div>
      </div>

      <div className="bg-white dark:bg-gray-800 rounded-lg shadow overflow-hidden">
        <div className="px-4 sm:px-6 py-4 border-b border-gray-200 dark:border-gray-700">
          <h2 className="text-lg font-medium text-gray-900 dark:text-white">
            Cases ({pagination.totalCount})
          </h2>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-700">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Case Details
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  User
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Documents
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Created
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
              {cases.map((caseItem) => (
                <tr key={caseItem.id} className="hover:bg-gray-50 dark:hover:bg-gray-700">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div>
                      <div className="text-sm font-medium text-gray-900 dark:text-white">
                        {caseItem.title}
                      </div>
                      <div className="text-sm text-gray-500 dark:text-gray-400">
                        {caseItem.caseNumber}
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div>
                      <div className="text-sm font-medium text-gray-900 dark:text-white">
                        {caseItem.userName}
                      </div>
                      <div className="text-sm text-gray-500 dark:text-gray-400">
                        {caseItem.userEmail}
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                      {caseItem.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                    {caseItem.documentCount} files
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                    {new Date(caseItem.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <div className="flex space-x-2">
                      <button
                        onClick={() => handleViewCase(caseItem.id)}
                        className="text-blue-600 hover:text-blue-900 dark:text-blue-400 dark:hover:text-blue-300"
                        title="View Case Details"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                        </svg>
                      </button>
                      <button
                        onClick={() => handleEditCase(caseItem.id)}
                        className="text-green-600 hover:text-green-900 dark:text-green-400 dark:hover:text-green-300"
                        title="Edit Case"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                      </button>
                      <button
                        onClick={() => handleDeleteCase(caseItem.id)}
                        className="text-red-600 hover:text-red-900 dark:text-red-400 dark:hover:text-red-300"
                        title="Delete Case"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {pagination.totalPages > 1 && (
          <div className="px-4 py-3 bg-gray-50 dark:bg-gray-700 border-t border-gray-200 dark:border-gray-600 sm:px-6">
            <div className="flex items-center justify-between">
              <div className="text-sm text-gray-700 dark:text-gray-300">
                Showing <span className="font-medium">{((pagination.page - 1) * pagination.pageSize) + 1}</span> to{' '}
                <span className="font-medium">
                  {Math.min(pagination.page * pagination.pageSize, pagination.totalCount)}
                </span>{' '}
                of <span className="font-medium">{pagination.totalCount}</span> results
              </div>
              <div className="flex space-x-2">
                <button
                  onClick={() => goToPage(pagination.page - 1)}
                  disabled={pagination.page <= 1}
                  className="relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed dark:bg-gray-600 dark:text-gray-300 dark:border-gray-600"
                >
                  Previous
                </button>
                <button
                  onClick={() => goToPage(pagination.page + 1)}
                  disabled={pagination.page >= pagination.totalPages}
                  className="relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed dark:bg-gray-600 dark:text-gray-300 dark:border-gray-600"
                >
                  Next
                </button>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Case Detail Modal */}
      {isDetailModalOpen && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-11/12 md:w-3/4 lg:w-2/3 shadow-lg rounded-md bg-white dark:bg-gray-800">
            <div className="mt-3">
              <div className="flex justify-between items-center pb-3 border-b">
                <h3 className="text-xl font-bold text-gray-900 dark:text-white">Case Details</h3>
                <button
                  onClick={() => setIsDetailModalOpen(false)}
                  className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
              
              {selectedCase && (
                <div className="mt-4 space-y-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Case Number</label>
                      <p className="mt-1 text-sm text-gray-900 dark:text-white">{selectedCase.caseNumber}</p>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Title</label>
                      <p className="mt-1 text-sm text-gray-900 dark:text-white">{selectedCase.title}</p>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Status</label>
                      <p className="mt-1 text-sm text-gray-900 dark:text-white capitalize">{selectedCase.status}</p>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">User</label>
                      <p className="mt-1 text-sm text-gray-900 dark:text-white">{selectedCase.userName} ({selectedCase.userEmail})</p>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Success Probability</label>
                      <p className="mt-1 text-sm text-gray-900 dark:text-white">
                        {selectedCase.successProbability ? `${(selectedCase.successProbability * 100).toFixed(1)}%` : 'N/A'}
                      </p>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Created</label>
                      <p className="mt-1 text-sm text-gray-900 dark:text-white">
                        {new Date(selectedCase.createdAt).toLocaleString()}
                      </p>
                    </div>
                  </div>
                  
                  {selectedCase.description && (
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Description</label>
                      <p className="mt-1 text-sm text-gray-900 dark:text-white whitespace-pre-wrap">{selectedCase.description}</p>
                    </div>
                  )}
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Documents ({selectedCase.documents.length})</label>
                    <div className="mt-2 space-y-2">
                      {selectedCase.documents.map((doc) => (
                        <div key={doc.id} className="flex justify-between items-center p-2 bg-gray-50 dark:bg-gray-700 rounded">
                          <div>
                            <p className="text-sm font-medium text-gray-900 dark:text-white">{doc.fileName}</p>
                            <p className="text-xs text-gray-500 dark:text-gray-400">
                              {doc.fileType} • {(doc.fileSize / 1024 / 1024).toFixed(2)} MB • {doc.status}
                            </p>
                          </div>
                          <span className="text-xs text-gray-500 dark:text-gray-400">
                            {new Date(doc.uploadedAt).toLocaleDateString()}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Analyses ({selectedCase.analyses.length})</label>
                    <div className="mt-2 space-y-2">
                      {selectedCase.analyses.map((analysis) => (
                        <div key={analysis.id} className="p-2 bg-gray-50 dark:bg-gray-700 rounded">
                          <div className="flex justify-between items-center">
                            <span className="text-sm font-medium text-gray-900 dark:text-white capitalize">{analysis.status}</span>
                            <span className="text-xs text-gray-500 dark:text-gray-400">
                              {new Date(analysis.createdAt).toLocaleString()}
                            </span>
                          </div>
                          {analysis.viabilityScore && (
                            <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                              Viability Score: {analysis.viabilityScore}%
                            </p>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              )}
              
              <div className="mt-6 flex justify-end space-x-3">
                <button
                  onClick={() => setIsDetailModalOpen(false)}
                  className="px-4 py-2 bg-gray-300 text-gray-700 rounded hover:bg-gray-400 dark:bg-gray-600 dark:text-gray-300 dark:hover:bg-gray-500"
                >
                  Close
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Edit Case Modal */}
      {isEditModalOpen && editingCase && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-11/12 md:w-1/2 lg:w-1/3 shadow-lg rounded-md bg-white dark:bg-gray-800">
            <div className="mt-3">
              <div className="flex justify-between items-center pb-3 border-b">
                <h3 className="text-xl font-bold text-gray-900 dark:text-white">Edit Case</h3>
                <button
                  onClick={() => setIsEditModalOpen(false)}
                  className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
              
              <div className="mt-4 space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Status</label>
                  <select
                    value={editingCase.status}
                    onChange={(e) => setEditingCase({...editingCase, status: e.target.value})}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white sm:text-sm"
                  >
                    <option value="draft">Draft</option>
                    <option value="new">New</option>
                    <option value="investigation">Investigation</option>
                    <option value="discovery">Discovery</option>
                    <option value="preTrial">Pre-Trial</option>
                    <option value="trial">Trial</option>
                    <option value="settlement">Settlement</option>
                    <option value="closed">Closed</option>
                    <option value="appealed">Appealed</option>
                    <option value="dismissed">Dismissed</option>
                  </select>
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Title</label>
                  <input
                    type="text"
                    value={editingCase.title}
                    onChange={(e) => setEditingCase({...editingCase, title: e.target.value})}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white sm:text-sm"
                  />
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Description</label>
                  <textarea
                    value={editingCase.description || ''}
                    onChange={(e) => setEditingCase({...editingCase, description: e.target.value})}
                    rows={3}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white sm:text-sm"
                  />
                </div>
              </div>
              
              <div className="mt-6 flex justify-end space-x-3">
                <button
                  onClick={() => setIsEditModalOpen(false)}
                  className="px-4 py-2 bg-gray-300 text-gray-700 rounded hover:bg-gray-400 dark:bg-gray-600 dark:text-gray-300 dark:hover:bg-gray-500"
                >
                  Cancel
                </button>
                <button
                  onClick={async () => {
                    try {
                      await updateCase(editingCase.id, {
                        title: editingCase.title,
                        description: editingCase.description,
                        status: editingCase.status
                      });
                      setIsEditModalOpen(false);
                    } catch (err) {
                      console.error('Error updating case:', err);
                    }
                  }}
                  className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600"
                >
                  Save Changes
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {isDeleteModalOpen && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-11/12 md:w-1/3 shadow-lg rounded-md bg-white dark:bg-gray-800">
            <div className="mt-3">
              <div className="flex justify-between items-center pb-3 border-b">
                <h3 className="text-xl font-bold text-gray-900 dark:text-white">Confirm Delete</h3>
                <button
                  onClick={() => setIsDeleteModalOpen(false)}
                  className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
              
              <div className="mt-4">
                <p className="text-sm text-gray-700 dark:text-gray-300">
                  Are you sure you want to delete this case? This action cannot be undone.
                </p>
              </div>
              
              <div className="mt-6 flex justify-end space-x-3">
                <button
                  onClick={() => setIsDeleteModalOpen(false)}
                  className="px-4 py-2 bg-gray-300 text-gray-700 rounded hover:bg-gray-400 dark:bg-gray-600 dark:text-gray-300 dark:hover:bg-gray-500"
                >
                  Cancel
                </button>
                <button
                  onClick={confirmDeleteCase}
                  className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600"
                >
                  Delete Case
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};