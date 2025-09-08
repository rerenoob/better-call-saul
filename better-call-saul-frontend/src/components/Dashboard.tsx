import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Case } from '../types/case';
import { caseService } from '../services/caseService';
import { AppLayout } from './layout/AppLayout';

export const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const [cases, setCases] = useState<Case[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadCases();
  }, []);

  const loadCases = async () => {
    try {
      setLoading(true);
      setError(null);
      const casesData = await caseService.getCases();
      setCases(casesData);
    } catch (error) {
      console.error('Failed to load cases:', error);
      setError(error instanceof Error ? error.message : 'Failed to load cases');
    } finally {
      setLoading(false);
    }
  };

  const getPriorityColor = (priority: string) => {
    switch (priority.toLowerCase()) {
      case 'high': return 'bg-green-100 text-green-800';
      case 'medium': return 'bg-yellow-100 text-yellow-800';
      case 'low': return 'bg-red-100 text-red-800';
      case 'urgent': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };



  const getSuccessProbabilityColor = (probability: number) => {
    if (probability >= 0.7) return 'text-green-600';
    if (probability >= 0.4) return 'text-yellow-600';
    return 'text-red-600';
  };

  const getSuccessBarColor = (probability: number) => {
    if (probability >= 0.7) return 'bg-green-500';
    if (probability >= 0.4) return 'bg-yellow-500';
    return 'bg-red-500';
  };

  const handleCaseClick = (caseId: string) => {
    navigate(`/cases/${caseId}`);
  };

  return (
    <AppLayout>
      <div className="bg-slate-100 min-h-screen">
        <main className="p-8">
          <h1 className="text-3xl font-bold text-slate-800 mb-6">Case Dashboard</h1>
          <div className="mb-6">
            
            {error && (
              <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg">
                <div className="flex">
                  <div className="flex-shrink-0">
                    <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                      <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                    </svg>
                  </div>
                  <div className="ml-3">
                    <h3 className="text-sm font-medium text-red-800">Error loading cases</h3>
                    <div className="mt-2 text-sm text-red-700">
                      <p>{error}</p>
                    </div>
                    <div className="mt-3">
                      <button
                        onClick={loadCases}
                        className="bg-red-100 px-3 py-1 rounded-md text-sm font-medium text-red-800 hover:bg-red-200"
                      >
                        Try again
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            )}
            
            {loading ? (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="bg-white rounded-lg shadow p-6 animate-pulse">
                    <div className="h-4 bg-gray-200 rounded mb-2"></div>
                    <div className="h-6 bg-gray-200 rounded mb-4"></div>
                    <div className="h-4 bg-gray-200 rounded w-3/4"></div>
                  </div>
                ))}
              </div>
            ) : cases.length === 0 ? (
              <div className="text-center py-12">
                <div className="mx-auto h-12 w-12 text-gray-400">
                  <svg fill="none" viewBox="0 0 24 24" stroke="currentColor" className="h-12 w-12">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                  </svg>
                </div>
                <h3 className="mt-2 text-sm font-medium text-gray-900">No cases</h3>
                <p className="mt-1 text-sm text-gray-500">Get started by creating your first case.</p>
                <div className="mt-6">
                  <button
                    type="button"
                    onClick={() => navigate('/cases/new')}
                    className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700"
                  >
                    <svg className="mr-2 h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                    </svg>
                    New Case
                  </button>
                </div>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {cases.map((caseItem) => (
                  <div
                    key={caseItem.id}
                    onClick={() => handleCaseClick(caseItem.id)}
                    className="bg-white p-6 rounded-xl shadow-md hover:shadow-lg transition-shadow cursor-pointer"
                  >
                    <div className="flex justify-between items-start mb-4">
                      <h2 className="text-xl font-bold text-slate-800">{caseItem.title}</h2>
                      <span className={`text-xs font-semibold px-2 py-1 rounded-full ${getPriorityColor(caseItem.priority || 'Medium')}`}>
                        {(caseItem.priority || 'Medium')} Priority
                      </span>
                    </div>
                    
                    <p className="text-sm text-slate-500 mb-4">
                      Client: {caseItem.description?.includes('Client:') 
                        ? caseItem.description.split('Client:')[1].split('.')[0].trim() 
                        : caseItem.description?.split(' - ')[0] || 'Not specified'}
                    </p>
                    
                    {caseItem.successProbability && (
                      <div className="space-y-3">
                        <div className="flex justify-between items-center text-sm">
                          <span className="text-slate-600 font-medium">AI Success Prediction</span>
                          <span className={`font-bold ${getSuccessProbabilityColor(caseItem.successProbability)}`}>
                            {(caseItem.successProbability * 100).toFixed(0)}%
                          </span>
                        </div>
                        <div className="w-full bg-slate-200 rounded-full h-2.5">
                          <div 
                            className={`h-2.5 rounded-full ${getSuccessBarColor(caseItem.successProbability)}`}
                            style={{ width: `${caseItem.successProbability * 100}%` }}
                          ></div>
                        </div>
                      </div>
                    )}
                    
                    <div className="mt-4 pt-4 border-t border-slate-200 text-right">
                      <button className="text-sm text-blue-600 font-semibold hover:underline">
                        View Analysis
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </main>
      </div>
    </AppLayout>
  );
};