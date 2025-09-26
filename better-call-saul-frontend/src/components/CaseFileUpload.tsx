import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { AppLayout } from './layout/AppLayout';
import { DragDropZone } from './upload/DragDropZone';
import { FilePreview } from './upload/FilePreview';
import { useFileUpload } from '../hooks/useFileUpload';
import { caseService } from '../services/caseService';
import { Case } from '../types/case';

export const CaseFileUpload: React.FC = () => {
  const navigate = useNavigate();
  const { id: caseId } = useParams<{ id: string }>();
  const [caseData, setCaseData] = useState<Case | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [error, setError] = useState('');

  const {
    files,
    isUploading,
    addFiles,
    removeFile,
    uploadFiles,
    retryFailedUploads,
    getUploadStats,
  } = useFileUpload();

  useEffect(() => {
    const loadCase = async () => {
      if (!caseId) {
        setError('Case ID not found');
        setIsLoading(false);
        return;
      }

      try {
        const caseInfo = await caseService.getCase(caseId);
        setCaseData(caseInfo.case);
      } catch (error) {
        console.error('Error loading case:', error);
        setError('Failed to load case information');
      } finally {
        setIsLoading(false);
      }
    };

    loadCase();
  }, [caseId]);

  const handleFilesSelected = (newFiles: File[]) => {
    const addedCount = addFiles(newFiles);
    if (addedCount === 0) {
      setError('All selected files were invalid or already added');
    } else {
      setError('');
    }
  };

  const handleUploadFiles = async () => {
    if (!caseId || files.length === 0) {
      setError('Please select files to upload');
      return;
    }

    setError('');

    try {
      // Upload files with the specific case ID
      const uploadResult = await uploadFiles(caseId);

      if (!uploadResult.success || uploadResult.fileIds.length === 0) {
        setError('Failed to upload files. Please try again.');
        return;
      }

      // Start AI analysis
      setIsAnalyzing(true);

      // Wait for AI analysis to complete
      setTimeout(() => {
        navigate(`/cases/${caseId}`);
      }, 8000); // Increased to 8 seconds to allow for OCR + AI analysis
    } catch (error) {
      console.error('Error uploading files:', error);
      setError('An unexpected error occurred. Please try again.');
    }
  };

  const handleSkipUpload = () => {
    if (caseId) {
      navigate(`/cases/${caseId}`);
    }
  };

  const stats = getUploadStats();
  const canUpload = caseId && files.length > 0 && !isUploading && !isAnalyzing;

  if (isLoading) {
    return (
      <AppLayout>
        <div className="min-h-screen flex items-center justify-center">
          <div className="text-center">
            <div className="inline-flex items-center justify-center w-16 h-16 bg-blue-100 rounded-full mb-4">
              <svg className="w-8 h-8 text-blue-600 animate-spin" fill="none" viewBox="0 0 24 24">
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
            </div>
            <p className="text-gray-600">Loading case information...</p>
          </div>
        </div>
      </AppLayout>
    );
  }

  if (error && !caseData) {
    return (
      <AppLayout>
        <div className="min-h-screen flex items-center justify-center">
          <div className="text-center">
            <div className="inline-flex items-center justify-center w-16 h-16 bg-red-100 rounded-full mb-4">
              <svg className="w-8 h-8 text-red-600" fill="currentColor" viewBox="0 0 20 20">
                <path
                  fillRule="evenodd"
                  d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                  clipRule="evenodd"
                />
              </svg>
            </div>
            <h2 className="text-xl font-bold text-gray-900 mb-2">Error Loading Case</h2>
            <p className="text-gray-600 mb-4">{error}</p>
            <button
              onClick={() => navigate('/dashboard')}
              className="px-4 py-2 text-white bg-blue-600 rounded-md hover:bg-blue-700"
            >
              Back to Dashboard
            </button>
          </div>
        </div>
      </AppLayout>
    );
  }

  if (isAnalyzing) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100">
        <div className="bg-white rounded-lg shadow-lg p-8 max-w-md w-full text-center">
          <div className="mb-6">
            <div className="inline-flex items-center justify-center w-16 h-16 bg-blue-100 rounded-full mb-4">
              <svg className="w-8 h-8 text-blue-600 animate-spin" fill="none" viewBox="0 0 24 24">
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
            </div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">AI Analysis in Progress...</h2>
            <p className="text-gray-600">
              Our AI is extracting text from your documents, analyzing the case content, and
              generating insights. This process takes a few moments to complete.
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
          <div className="mb-6">
            <h1 className="text-2xl font-bold text-gray-900 mb-2">Upload Files to Case</h1>
            {caseData && (
              <div className="text-gray-600">
                <p><strong>Case:</strong> {caseData.title}</p>
                <p><strong>Client:</strong> {caseData.description?.replace('Client: ', '') || 'Not specified'}</p>
              </div>
            )}
          </div>

          {/* File Upload Section */}
          <div className="mb-8">
            <label className="block text-sm font-medium text-gray-700 mb-4">
              Upload Case Files
            </label>
            <DragDropZone
              onFilesSelected={handleFilesSelected}
              disabled={isUploading || isAnalyzing}
              maxFiles={10}
            />

            <FilePreview
              files={files}
              onRemoveFile={removeFile}
              onRetryUpload={() => retryFailedUploads(caseId)}
            />
          </div>

          {/* Upload Stats */}
          {files.length > 0 && (
            <div className="mb-6 p-4 bg-gray-50 rounded-lg">
              <div className="flex items-center justify-between text-sm text-gray-600">
                <span>Files: {stats.total}</span>
                {stats.completed > 0 && (
                  <span className="text-green-600">Completed: {stats.completed}</span>
                )}
                {stats.failed > 0 && <span className="text-red-600">Failed: {stats.failed}</span>}
                {stats.uploading > 0 && (
                  <span className="text-blue-600">Uploading: {stats.uploading}</span>
                )}
              </div>
            </div>
          )}

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
          <div className="flex justify-between space-x-4">
            <div className="flex space-x-4">
              <button
                onClick={() => navigate('/dashboard')}
                className="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500"
                disabled={isUploading || isAnalyzing}
              >
                Back to Dashboard
              </button>
              
              <button
                onClick={handleSkipUpload}
                className="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500"
                disabled={isUploading || isAnalyzing}
              >
                Skip Upload
              </button>
            </div>

            <button
              onClick={handleUploadFiles}
              disabled={!canUpload}
              className={`px-6 py-2 text-white rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 flex items-center ${
                canUpload ? 'bg-blue-600 hover:bg-blue-700' : 'bg-gray-400 cursor-not-allowed'
              }`}
            >
              {isUploading ? (
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
                  Uploading...
                </>
              ) : (
                <>
                  <svg className="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M13 6a3 3 0 11-6 0 3 3 0 016 0zM18 8a2 2 0 11-4 0 2 2 0 014 0zM14 15a4 4 0 00-8 0v3h8v-3z" />
                  </svg>
                  Upload Files & Start Analysis
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </AppLayout>
  );
};