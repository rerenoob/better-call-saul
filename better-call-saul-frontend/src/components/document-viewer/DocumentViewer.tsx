import React, { useState, useEffect, useCallback } from 'react';
import { adminService, DocumentViewerData, DocumentPageData } from '../../services/adminService';

interface DocumentViewerProps {
  documentId: string;
  onClose: () => void;
}

export const DocumentViewer: React.FC<DocumentViewerProps> = ({ documentId, onClose }) => {
  const [documentData, setDocumentData] = useState<DocumentViewerData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<number[]>([]);
  const [currentSearchIndex, setCurrentSearchIndex] = useState(-1);

  const loadDocumentContent = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await adminService.getDocumentContent(documentId);
      setDocumentData(data);
      setCurrentPage(1);
    } catch (err) {
      setError('Failed to load document content');
      console.error('Error loading document:', err);
    } finally {
      setIsLoading(false);
    }
  }, [documentId]);

  useEffect(() => {
    loadDocumentContent();
  }, [loadDocumentContent]);

  const handleSearch = async () => {
    if (!searchQuery.trim() || !documentData) return;

    try {
      const result = await adminService.searchDocument(documentId, searchQuery);
      const pageNumbers = [...new Set(result.matches.map(match => match.pageNumber))];
      setSearchResults(pageNumbers);
      setCurrentSearchIndex(0);
      
      if (pageNumbers.length > 0) {
        setCurrentPage(pageNumbers[0]);
      }
    } catch (err) {
      console.error('Error searching document:', err);
    }
  };

  const navigateSearch = (direction: 'next' | 'prev') => {
    if (searchResults.length === 0) return;
    
    let newIndex;
    if (direction === 'next') {
      newIndex = (currentSearchIndex + 1) % searchResults.length;
    } else {
      newIndex = (currentSearchIndex - 1 + searchResults.length) % searchResults.length;
    }
    
    setCurrentSearchIndex(newIndex);
    setCurrentPage(searchResults[newIndex]);
  };

  const getCurrentPageData = (): DocumentPageData | undefined => {
    return documentData?.pages.find(page => page.pageNumber === currentPage);
  };

  const highlightSearchTerms = (text: string): string => {
    if (!searchQuery.trim()) return text;
    
    const regex = new RegExp(`(${searchQuery.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
    return text.replace(regex, '<mark class="bg-yellow-200 dark:bg-yellow-600">$1</mark>');
  };

  if (isLoading) {
    return (
      <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
        <div className="relative top-20 mx-auto p-5 border w-11/12 md:w-3/4 lg:w-2/3 shadow-lg rounded-md bg-white dark:bg-gray-800">
          <div className="flex items-center justify-center min-h-64">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
        <div className="relative top-20 mx-auto p-5 border w-11/12 md:w-3/4 lg:w-2/3 shadow-lg rounded-md bg-white dark:bg-gray-800">
          <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
            <div className="flex items-center space-x-2">
              <svg className="w-5 h-5 text-red-600" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              <p className="text-sm font-medium text-red-800 dark:text-red-300">Error Loading Document</p>
            </div>
            <p className="mt-1 text-sm text-red-700 dark:text-red-400">{error}</p>
            <button
              onClick={loadDocumentContent}
              className="mt-2 px-3 py-1 bg-red-600 text-white text-sm rounded hover:bg-red-700"
            >
              Retry
            </button>
          </div>
          <div className="mt-4 flex justify-end">
            <button
              onClick={onClose}
              className="px-4 py-2 bg-gray-300 text-gray-700 rounded hover:bg-gray-400 dark:bg-gray-600 dark:text-gray-300 dark:hover:bg-gray-500"
            >
              Close
            </button>
          </div>
        </div>
      </div>
    );
  }

  const currentPageData = getCurrentPageData();

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
      <div className="relative top-4 mx-auto p-5 border w-11/12 md:w-5/6 lg:w-4/5 shadow-lg rounded-md bg-white dark:bg-gray-800 max-h-[95vh] flex flex-col">
        {/* Header */}
        <div className="flex justify-between items-center pb-3 border-b">
          <div>
            <h3 className="text-xl font-bold text-gray-900 dark:text-white">Document Viewer</h3>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              {documentData?.fileName} • {documentData?.caseNumber}
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Search Bar */}
        <div className="mt-4 flex space-x-2">
          <input
            type="text"
            placeholder="Search in document..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            className="flex-1 rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white sm:text-sm"
          />
          <button
            onClick={handleSearch}
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600"
          >
            Search
          </button>
          {searchResults.length > 0 && (
            <div className="flex items-center space-x-2">
              <span className="text-sm text-gray-500 dark:text-gray-400">
                {currentSearchIndex + 1} of {searchResults.length}
              </span>
              <button
                onClick={() => navigateSearch('prev')}
                className="px-2 py-1 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 dark:bg-gray-600 dark:text-gray-300 dark:hover:bg-gray-500"
              >
                ←
              </button>
              <button
                onClick={() => navigateSearch('next')}
                className="px-2 py-1 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 dark:bg-gray-600 dark:text-gray-300 dark:hover:bg-gray-500"
              >
                →
              </button>
            </div>
          )}
        </div>

        {/* Document Content */}
        <div className="mt-4 flex-1 overflow-auto">
          {currentPageData ? (
            <div className="bg-gray-50 dark:bg-gray-900 rounded-lg p-6">
              <div className="flex justify-between items-center mb-4">
                <h4 className="text-lg font-semibold text-gray-900 dark:text-white">
                  Page {currentPage} of {documentData?.pageCount}
                </h4>
                <div className="text-sm text-gray-500 dark:text-gray-400">
                  Confidence: {(currentPageData.confidence * 100).toFixed(1)}%
                </div>
              </div>
              
              <div className="bg-white dark:bg-gray-800 rounded border p-4 min-h-96">
                {currentPageData.text ? (
                  <div 
                    className="prose prose-sm max-w-none dark:prose-invert"
                    dangerouslySetInnerHTML={{
                      __html: highlightSearchTerms(currentPageData.text)
                    }}
                  />
                ) : (
                  <div className="text-center text-gray-500 dark:text-gray-400 py-8">
                    <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                    </svg>
                    <p className="mt-2">No text extracted from this page</p>
                  </div>
                )}
              </div>
            </div>
          ) : (
            <div className="text-center text-gray-500 dark:text-gray-400 py-8">
              <p>No content available for this page</p>
            </div>
          )}
        </div>

        {/* Pagination Controls */}
        <div className="mt-4 flex justify-between items-center">
          <button
            onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
            disabled={currentPage <= 1}
            className="px-4 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 disabled:opacity-50 disabled:cursor-not-allowed dark:bg-gray-600 dark:text-gray-300 dark:hover:bg-gray-500"
          >
            Previous Page
          </button>
          
          <div className="flex items-center space-x-2">
            <span className="text-sm text-gray-500 dark:text-gray-400">
              Page {currentPage} of {documentData?.pageCount || 1}
            </span>
            <select
              value={currentPage}
              onChange={(e) => setCurrentPage(parseInt(e.target.value))}
              className="rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white sm:text-sm"
            >
              {Array.from({ length: documentData?.pageCount || 0 }, (_, i) => i + 1).map(page => (
                <option key={page} value={page}>
                  {page}
                </option>
              ))}
            </select>
          </div>
          
          <button
            onClick={() => setCurrentPage(Math.min(documentData?.pageCount || 1, currentPage + 1))}
            disabled={currentPage >= (documentData?.pageCount || 1)}
            className="px-4 py-2 bg-gray-200 text-gray-700 rounded hover:bg-gray-300 disabled:opacity-50 disabled:cursor-not-allowed dark:bg-gray-600 dark:text-gray-300 dark:hover:bg-gray-500"
          >
            Next Page
          </button>
        </div>

        {/* Full Text View Toggle */}
        {documentData?.extractedText && (
          <div className="mt-4">
            <details className="bg-gray-50 dark:bg-gray-900 rounded-lg">
              <summary className="cursor-pointer p-3 font-medium text-gray-900 dark:text-white">
                View Full Extracted Text
              </summary>
              <div className="p-3 border-t">
                <div 
                  className="prose prose-sm max-w-none dark:prose-invert bg-white dark:bg-gray-800 p-4 rounded max-h-64 overflow-auto"
                  dangerouslySetInnerHTML={{
                    __html: highlightSearchTerms(documentData.extractedText || '')
                  }}
                />
              </div>
            </details>
          </div>
        )}
      </div>
    </div>
  );
};