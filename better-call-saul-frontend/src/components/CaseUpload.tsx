import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AppLayout } from './layout/AppLayout';
import { DragDropZone } from './upload/DragDropZone';
import { FilePreview } from './upload/FilePreview';
import { useFileUpload } from '../hooks/useFileUpload';
import { fileUploadService } from '../services/fileUploadService';

export const CaseUpload: React.FC = () => {
  const navigate = useNavigate();
  const [caseName, setCaseName] = useState('');
  const [clientName, setClientName] = useState('');
  const [isCreatingCase, setIsCreatingCase] = useState(false);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [error, setError] = useState('');

  const {
    files,
    isUploading,
    addFiles,
    removeFile,
    uploadFiles,
    retryFailedUploads,
    getUploadStats
  } = useFileUpload();

  const handleFilesSelected = (newFiles: File[]) => {
    const addedCount = addFiles(newFiles);
    if (addedCount === 0) {
      setError('All selected files were invalid or already added');
    } else {
      setError('');
    }
  };

  const handleAnalyzeCase = async () => {
    if (!caseName.trim() || !clientName.trim()) {
      setError('Please enter both case name and client name');
      return;
    }

    if (files.length === 0) {
      setError('Please upload at least one case file');
      return;
    }

    setError('');
    setIsCreatingCase(true);

    try {
      // First upload all files
      const uploadResult = await uploadFiles();
      
      if (!uploadResult.success || uploadResult.fileIds.length === 0) {
        setError('Failed to upload files. Please try again.');
        setIsCreatingCase(false);
        return;
      }

      // Create case with uploaded files
      const createCaseResult = await fileUploadService.createCaseWithFiles(
        caseName.trim(),
        clientName.trim(),
        uploadResult.fileIds
      );

      if (createCaseResult.success && createCaseResult.caseId) {
        setIsCreatingCase(false);
        setIsAnalyzing(true);

        // Wait for AI analysis to complete (with real polling or just a reasonable delay)
        setTimeout(() => {
          navigate(`/cases/${createCaseResult.caseId}`);
        }, 8000); // Increased to 8 seconds to allow for OCR + AI analysis
      } else {
        setError(createCaseResult.error || 'Failed to create case. Please try again.');
        setIsCreatingCase(false);
      }
    } catch (error) {
      console.error('Error creating case:', error);
      setError('An unexpected error occurred. Please try again.');
      setIsCreatingCase(false);
    }
  };

  const stats = getUploadStats();
  const canAnalyze = caseName.trim() && clientName.trim() && files.length > 0 && !isUploading && !isCreatingCase;

  if (isAnalyzing) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100">
        <div className="bg-white rounded-lg shadow-lg p-8 max-w-md w-full text-center">
          <div className="mb-6">
            <div className="inline-flex items-center justify-center w-16 h-16 bg-blue-100 rounded-full mb-4">
              <svg className="w-8 h-8 text-blue-600 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="m4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">
              AI Analysis in Progress...
            </h2>
            <p className="text-gray-600">
              Our AI is extracting text from your documents, analyzing the case content, and generating insights. 
              This process takes a few moments to complete.
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <AppLayout>
      <div className="p-6 max-w-4xl mx-auto">
        <div className="bg-white rounded-lg shadow-lg p-6">
          <h1 className="text-2xl font-bold text-gray-900 mb-6">Add New Case</h1>

          {/* Case Information Form */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
            <div>
              <label htmlFor="caseName" className="block text-sm font-medium text-gray-700 mb-2">
                Case Name
              </label>
              <input
                id="caseName"
                type="text"
                value={caseName}
                onChange={(e) => setCaseName(e.target.value)}
                placeholder="e.g., State v. Anderson"
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                disabled={isCreatingCase}
              />
            </div>

            <div>
              <label htmlFor="clientName" className="block text-sm font-medium text-gray-700 mb-2">
                Client Name
              </label>
              <input
                id="clientName"
                type="text"
                value={clientName}
                onChange={(e) => setClientName(e.target.value)}
                placeholder="e.g., David Anderson"
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                disabled={isCreatingCase}
              />
            </div>
          </div>

          {/* File Upload Section */}
          <div className="mb-8">
            <label className="block text-sm font-medium text-gray-700 mb-4">
              Upload Case Files
            </label>
            <DragDropZone
              onFilesSelected={handleFilesSelected}
              disabled={isCreatingCase}
              maxFiles={10}
            />
            
            <FilePreview
              files={files}
              onRemoveFile={removeFile}
              onRetryUpload={retryFailedUploads}
            />
          </div>

          {/* Upload Stats */}
          {files.length > 0 && (
            <div className="mb-6 p-4 bg-gray-50 rounded-lg">
              <div className="flex items-center justify-between text-sm text-gray-600">
                <span>Files: {stats.total}</span>
                {stats.completed > 0 && <span className="text-green-600">Completed: {stats.completed}</span>}
                {stats.failed > 0 && <span className="text-red-600">Failed: {stats.failed}</span>}
                {stats.uploading > 0 && <span className="text-blue-600">Uploading: {stats.uploading}</span>}
              </div>
            </div>
          )}

          {/* Error Message */}
          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-md">
              <div className="flex">
                <svg className="w-5 h-5 text-red-400 mr-2" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
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
              disabled={isCreatingCase}
            >
              Cancel
            </button>

            <button
              onClick={handleAnalyzeCase}
              disabled={!canAnalyze}
              className={`px-6 py-2 text-white rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 flex items-center ${
                canAnalyze
                  ? 'bg-blue-600 hover:bg-blue-700'
                  : 'bg-gray-400 cursor-not-allowed'
              }`}
            >
              {isCreatingCase ? (
                <>
                  <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="m4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  Creating Case...
                </>
              ) : (
                <>
                  <svg className="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M13 6a3 3 0 11-6 0 3 3 0 016 0zM18 8a2 2 0 11-4 0 2 2 0 014 0zM14 15a4 4 0 00-8 0v3h8v-3z" />
                  </svg>
                  Analyze Case
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </AppLayout>
  );
};