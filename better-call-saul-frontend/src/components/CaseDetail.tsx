import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Case } from '../types/case';
import { caseService } from '../services/caseService';
import { useAuth } from '../hooks/useAuth';

interface ChatMessage {
  id: string;
  message: string;
  isAI: boolean;
  timestamp: Date;
}

export const CaseDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [caseData, setCaseData] = useState<Case | null>(null);
  const [loading, setLoading] = useState(true);
  const [chatMessages, setChatMessages] = useState<ChatMessage[]>([]);
  const [currentMessage, setCurrentMessage] = useState('');
  const [isAITyping, setIsAITyping] = useState(false);
  const [relevantCaseLaw, setRelevantCaseLaw] = useState<{caseName: string; summary: string}[]>([]);

  useEffect(() => {
    if (id) {
      loadCaseData(id);
    }
  }, [id]); // eslint-disable-line react-hooks/exhaustive-deps

  const loadCaseData = async (caseId: string) => {
    try {
      setLoading(true);
      const caseDetails = await caseService.getCase(caseId);
      setCaseData(caseDetails);
      
      // Load case analysis data
      await loadCaseAnalysis();
      
      // Load relevant case law
      await loadRelevantCaseLaw();
    } catch (error) {
      console.error('Failed to load case data:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadCaseAnalysis = async () => {
    try {
      // This would integrate with the case analysis API endpoint
      // For now, we'll use the case data itself
      // Analysis data is already in caseData
    } catch (error) {
      console.error('Failed to load case analysis:', error);
    }
  };

  const loadRelevantCaseLaw = async () => {
    try {
      // This would integrate with the legal research API
      // For production, this should call an actual legal research service
      setRelevantCaseLaw([]); // Empty array for now - will be populated by API
    } catch (error) {
      console.error('Failed to load relevant case law:', error);
    }
  };

  const handleSendMessage = async () => {
    if (!currentMessage.trim() || !id) return;

    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      message: currentMessage,
      isAI: false,
      timestamp: new Date()
    };

    setChatMessages(prev => [...prev, userMessage]);
    setCurrentMessage('');
    setIsAITyping(true);

    try {
      const response = await caseService.chatWithAI(id, currentMessage);
      
      const aiMessage: ChatMessage = {
        id: (Date.now() + 1).toString(),
        message: response.generatedText || 'I apologize, but I encountered an error processing your request.',
        isAI: true,
        timestamp: new Date()
      };

      setChatMessages(prev => [...prev, aiMessage]);
    } catch (error: unknown) {
      let errorMessageText = 'I apologize, but I encountered an error processing your request. Please try again.';
      
      if (typeof error === 'object' && error !== null && 'response' in error) {
        const responseError = error as { response?: { data?: { error?: string } } };
        errorMessageText = responseError.response?.data?.error || errorMessageText;
      }
      
      const errorMessage: ChatMessage = {
        id: (Date.now() + 1).toString(),
        message: errorMessageText,
        isAI: true,
        timestamp: new Date()
      };
      setChatMessages(prev => [...prev, errorMessage]);
    } finally {
      setIsAITyping(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };



  const getRecommendationClass = (probability?: number) => {
    if (!probability) return 'bg-gray-50 border-gray-200 text-gray-800';
    if (probability >= 0.7) return 'bg-green-50 border-green-200 text-green-800';
    if (probability >= 0.5) return 'bg-yellow-50 border-yellow-200 text-yellow-800';
    return 'bg-red-50 border-red-200 text-red-800';
  };

  const getSuccessProbabilityColor = (probability: number) => {
    if (probability >= 0.7) return 'text-green-600';
    if (probability >= 0.5) return 'text-yellow-600';
    return 'text-red-600';
  };

  const getRecommendationText = (probability?: number) => {
    if (!probability) return 'Analysis pending';
    if (probability >= 0.7) return 'Proceed to Trial';
    if (probability >= 0.5) return 'Negotiate Plea Deal';
    return 'Recommend Plea Deal';
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-500 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading case details...</p>
        </div>
      </div>
    );
  }

  if (!caseData) {
    return (
      <div className="min-h-screen bg-gray-100 flex items-center justify-center">
        <div className="text-center">
          <p className="text-xl text-gray-600">Case not found</p>
          <button 
            onClick={() => navigate('/dashboard')}
            className="mt-4 px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
          >
            Back to Dashboard
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-100 flex">
      {/* Sidebar - using inline sidebar to match the mockup exactly */}
      <aside className="w-64 bg-slate-800 text-white flex flex-col">
        <div className="p-6 text-2xl font-bold border-b border-slate-700 flex items-center">
          <svg className="w-6 h-6 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 6l3 1m0 0l-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1l-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3" />
          </svg>
          <span>BCS AI</span>
        </div>
        <nav className="flex-1 p-4 space-y-2">
          <button
            onClick={() => navigate('/dashboard')}
            className="flex items-center px-4 py-2 rounded-lg text-sm font-medium bg-slate-700 text-white w-full text-left"
          >
            <svg className="mr-3 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
            </svg>
            Dashboard
          </button>
          <button
            onClick={() => navigate('/cases/new')}
            className="flex items-center px-4 py-2 rounded-lg text-sm font-medium text-slate-300 hover:text-white hover:bg-slate-700 w-full text-left"
          >
            <svg className="mr-3 h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 13h6m-3-3v6m5 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            Add New Case
          </button>
        </nav>
        <div className="p-4 border-t border-slate-700">
          <div className="flex items-center">
            <div className="w-10 h-10 bg-blue-500 rounded-full flex items-center justify-center text-white font-semibold text-sm">
              PD
            </div>
            <div className="ml-3">
              <p className="text-sm font-semibold">{user?.name || 'User'}</p>
              <p className="text-xs text-slate-400">{user?.role || 'Public Defender'}</p>
            </div>
          </div>
          <button
            className="w-full mt-4 flex items-center justify-center text-sm text-slate-300 hover:text-white bg-slate-700/50 hover:bg-slate-700 py-2 rounded-lg transition-colors"
          >
            <svg className="mr-2 h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
            Logout
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1">
        {/* Header */}
        <header className="bg-white border-b border-slate-200 p-6">
          <button 
            onClick={() => navigate('/dashboard')}
            className="flex items-center text-sm font-semibold text-slate-600 hover:text-slate-900 mb-2"
          >
            <svg className="mr-2 h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
            Back to Dashboard
          </button>
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-3xl font-bold text-slate-800">{caseData.title}</h1>
              <p className="text-slate-500 mt-1">
                Case Number: {caseData.caseNumber}
              </p>
            </div>
            <div className="text-right">
              <p className="text-sm text-slate-500">AI Success Prediction</p>
              <p className={`text-4xl font-bold ${getSuccessProbabilityColor(caseData.successProbability || 0)}`}>
                {caseData.successProbability ? `${(caseData.successProbability * 100).toFixed(0)}%` : 'N/A'}
              </p>
            </div>
          </div>
        </header>

        {/* Content */}
        <div className="p-8">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Left Column: Summary & Recommendation */}
            <div className="lg:col-span-2 space-y-6">

              {/* Case Summary */}
              <div>
                <h3 className="text-xl font-semibold text-slate-700 mb-3 flex items-center">
                  <svg className="mr-2 h-5 w-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                  </svg>
                  AI Case Summary
                </h3>
                <p className="text-slate-600 leading-relaxed">
                  {caseData.description || 'No case summary available. Please analyze this case to generate an AI summary.'}
                </p>
              </div>

              {/* AI Recommendation */}
              <div>
                <h3 className="text-xl font-semibold text-slate-700 mb-3 flex items-center">
                  <svg className="mr-2 h-5 w-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 6l3 1m0 0l-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1l-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3" />
                  </svg>
                  AI Recommendation
                </h3>
                <div className={`p-6 rounded-lg border ${getRecommendationClass(caseData.successProbability)}`}>
                  <p className="font-bold text-lg text-blue-800">{getRecommendationText(caseData.successProbability)}</p>
                  <p className="text-blue-700 mt-2">
                    {caseData.successProbability && caseData.successProbability >= 0.7 
                      ? 'The case presents a strong potential for a successful defense. The weaknesses in the prosecution\'s evidence provide multiple avenues for reasonable doubt. A plea deal is not recommended at this stage as it undervalues the defensive position.'
                      : caseData.successProbability && caseData.successProbability >= 0.5
                      ? 'The outcome of a trial is uncertain. Negotiating for a reduced charge from a position of strength, leveraging the procedural errors, is advised. A plea bargain could mitigate risk while securing a favorable outcome compared to a potential conviction at trial.'
                      : 'Given the low probability of success at trial, the most advantageous strategy is to secure the best possible plea agreement. Focus should be on mitigating sentencing and avoiding the risks of a conviction on more severe charges.'
                    }
                  </p>
                </div>
              </div>

              {/* Success Probability Chart */}
              <div className="bg-white rounded-lg shadow p-6">
                <h3 className="text-xl font-semibold text-gray-700 mb-4 flex items-center">
                  <span className="mr-2">📊</span>
                  Success Probability Analysis
                </h3>
                <div className="space-y-4">
                  <div>
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-sm font-medium text-gray-600">Success Probability</span>
                      <span className="text-lg font-bold text-green-600">
                        {caseData.successProbability ? `${(caseData.successProbability * 100).toFixed(0)}%` : 'N/A'}
                      </span>
                    </div>
                    {caseData.successProbability && (
                      <div className="w-full bg-gray-200 rounded-full h-3">
                        <div 
                          className="bg-green-600 h-3 rounded-full transition-all duration-500" 
                          style={{ width: `${caseData.successProbability * 100}%` }}
                        ></div>
                      </div>
                    )}
                  </div>
                  {caseData.estimatedValue && (
                    <div>
                      <div className="flex items-center justify-between mb-2">
                        <span className="text-sm font-medium text-gray-600">Estimated Value</span>
                        <span className="text-lg font-bold text-blue-600">
                          ${caseData.estimatedValue.toLocaleString()}
                        </span>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Right Column: Ask AI Chat & Case Law */}
          <div className="space-y-6">
            {/* Relevant Case Law */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-xl font-semibold text-gray-700 mb-4 flex items-center">
                <span className="mr-2">📚</span>
                Relevant Case Law
              </h3>
              <div className="space-y-4">
                {relevantCaseLaw.length > 0 ? (
                  relevantCaseLaw.map((caseLaw, index) => (
                    <div key={index} className="bg-gray-50 p-4 rounded-lg hover:bg-gray-100 transition-colors cursor-pointer">
                      <p className="font-semibold text-gray-800">{caseLaw.caseName}</p>
                      <p className="text-sm text-gray-500 mt-1">{caseLaw.summary}</p>
                    </div>
                  ))
                ) : (
                  <div className="text-center text-gray-500 text-sm py-8">
                    No relevant case law found. Analyze this case to discover applicable precedents.
                  </div>
                )}
              </div>
            </div>

            {/* Ask AI Chat */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-xl font-semibold text-gray-700 mb-4 flex items-center">
                <span className="mr-2">🤖</span>
                Ask the AI
              </h3>
              
              {/* Chat Messages */}
              <div className="h-64 overflow-y-auto border border-gray-200 rounded-lg p-3 mb-4">
                {chatMessages.length === 0 ? (
                  <div className="text-center text-gray-500 text-sm">
                    Ask me anything about this case...
                  </div>
                ) : (
                  chatMessages.map((msg) => (
                    <div
                      key={msg.id}
                      className={`mb-3 ${msg.isAI ? 'text-left' : 'text-right'}`}
                    >
                      <div
                        className={`inline-block p-2 rounded-lg max-w-xs ${
                          msg.isAI
                            ? 'bg-gray-100 text-gray-800'
                            : 'bg-blue-500 text-white'
                        }`}
                      >
                        <p className="text-sm">{msg.message}</p>
                        <p className="text-xs mt-1 opacity-70">
                          {msg.timestamp.toLocaleTimeString()}
                        </p>
                      </div>
                    </div>
                  ))
                )}
                {isAITyping && (
                  <div className="text-left mb-3">
                    <div className="inline-block p-2 rounded-lg bg-gray-100 text-gray-800">
                      <p className="text-sm">AI is thinking...</p>
                    </div>
                  </div>
                )}
              </div>

              {/* Chat Input */}
              <div className="relative">
                <textarea
                  value={currentMessage}
                  onChange={(e) => setCurrentMessage(e.target.value)}
                  onKeyPress={handleKeyPress}
                  placeholder="Ask a question about this case..."
                  className="w-full p-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                  rows={3}
                  disabled={isAITyping}
                />
                <button
                  onClick={handleSendMessage}
                  disabled={!currentMessage.trim() || isAITyping}
                  className="absolute bottom-2 right-2 bg-blue-600 text-white rounded-md p-2 hover:bg-blue-700 disabled:bg-gray-400"
                >
                  Send
                </button>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};