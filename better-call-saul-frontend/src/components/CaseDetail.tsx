import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Case, CaseStatus, CaseType, CasePriority } from '../types/case';
import { caseService } from '../services/caseService';

interface ChatMessage {
  id: string;
  message: string;
  isAI: boolean;
  timestamp: Date;
}

export const CaseDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [caseData, setCaseData] = useState<Case | null>(null);
  const [loading, setLoading] = useState(true);
  const [chatMessages, setChatMessages] = useState<ChatMessage[]>([]);
  const [currentMessage, setCurrentMessage] = useState('');
  const [isAITyping, setIsAITyping] = useState(false);

  useEffect(() => {
    if (id) {
      loadCaseData(id);
    }
  }, [id]);

  const loadCaseData = async (caseId: string) => {
    try {
      setLoading(true);
      const caseDetails = await caseService.getCase(caseId);
      setCaseData(caseDetails);
    } catch (error) {
      console.error('Failed to load case data:', error);
    } finally {
      setLoading(false);
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
    } catch (error) {
      console.error('Failed to get AI response:', error);
      const errorMessage: ChatMessage = {
        id: (Date.now() + 1).toString(),
        message: 'I apologize, but I encountered an error processing your request. Please try again.',
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

  const getStatusColor = (status: CaseStatus) => {
    switch (status) {
      case CaseStatus.NEW: return 'bg-blue-100 text-blue-800';
      case CaseStatus.INVESTIGATION: return 'bg-purple-100 text-purple-800';
      case CaseStatus.DISCOVERY: return 'bg-yellow-100 text-yellow-800';
      case CaseStatus.PRETRIAL: return 'bg-orange-100 text-orange-800';
      case CaseStatus.TRIAL: return 'bg-red-100 text-red-800';
      case CaseStatus.SETTLEMENT: return 'bg-green-100 text-green-800';
      case CaseStatus.CLOSED: return 'bg-gray-100 text-gray-800';
      case CaseStatus.APPEALED: return 'bg-indigo-100 text-indigo-800';
      case CaseStatus.DISMISSED: return 'bg-pink-100 text-pink-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getPriorityColor = (priority: CasePriority) => {
    switch (priority) {
      case CasePriority.LOW: return 'bg-green-100 text-green-800';
      case CasePriority.MEDIUM: return 'bg-yellow-100 text-yellow-800';
      case CasePriority.HIGH: return 'bg-orange-100 text-orange-800';
      case CasePriority.URGENT: return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getTypeColor = (type: CaseType) => {
    switch (type) {
      case CaseType.CRIMINAL: return 'bg-red-100 text-red-800';
      case CaseType.CIVIL: return 'bg-blue-100 text-blue-800';
      case CaseType.FAMILY: return 'bg-purple-100 text-purple-800';
      case CaseType.IMMIGRATION: return 'bg-green-100 text-green-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getRecommendationClass = (probability?: number) => {
    if (!probability) return 'bg-gray-50 border-gray-200 text-gray-800';
    if (probability >= 0.7) return 'bg-green-50 border-green-200 text-green-800';
    if (probability >= 0.5) return 'bg-yellow-50 border-yellow-200 text-yellow-800';
    return 'bg-red-50 border-red-200 text-red-800';
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
    <div className="min-h-screen bg-gray-100">
      <header className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <div className="flex items-center">
            <button 
              onClick={() => navigate('/dashboard')}
              className="mr-4 text-gray-600 hover:text-gray-900"
            >
              ← Back to Dashboard
            </button>
            <h1 className="text-2xl font-bold text-gray-900">{caseData.title}</h1>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Case Information */}
          <div className="lg:col-span-2 space-y-6">
            {/* Case Header */}
            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex justify-between items-start mb-6 pb-6 border-b border-gray-200">
                <div>
                  <h2 className="text-3xl font-bold text-gray-900">{caseData.title}</h2>
                  <p className="text-gray-600 mt-1">Case #: {caseData.caseNumber || caseData.id}</p>
                  {caseData.description && (
                    <p className="mt-2 text-gray-700">{caseData.description}</p>
                  )}
                </div>
                <div className="text-right">
                  <p className="text-sm text-gray-500">AI Success Prediction</p>
                  <p className="text-4xl font-bold text-green-600">
                    {caseData.successProbability ? `${(caseData.successProbability * 100).toFixed(0)}%` : 'N/A'}
                  </p>
                </div>
              </div>
              
              <div className="grid grid-cols-2 md:grid-cols-4 gap-6 mt-6">
                <div>
                  <p className="text-sm font-medium text-gray-500">Status</p>
                  <span className={`mt-1 inline-block px-3 py-1 rounded-full text-sm font-medium ${getStatusColor(caseData.status)}`}>
                    {caseData.status}
                  </span>
                </div>
                <div>
                  <p className="text-sm font-medium text-gray-500">Priority</p>
                  <span className={`mt-1 inline-block px-3 py-1 rounded-full text-sm font-medium ${getPriorityColor(caseData.priority)}`}>
                    {caseData.priority}
                  </span>
                </div>
                <div>
                  <p className="text-sm font-medium text-gray-500">Type</p>
                  <span className={`mt-1 inline-block px-3 py-1 rounded-full text-sm font-medium ${getTypeColor(caseData.type)}`}>
                    {caseData.type}
                  </span>
                </div>
                <div>
                  <p className="text-sm font-medium text-gray-500">Court</p>
                  <p className="mt-1 text-sm text-gray-900">{caseData.court || 'N/A'}</p>
                </div>
                <div>
                  <p className="text-sm font-medium text-gray-500">Judge</p>
                  <p className="mt-1 text-sm text-gray-900">{caseData.judge || 'N/A'}</p>
                </div>
                <div>
                  <p className="text-sm font-medium text-gray-500">Filed Date</p>
                  <p className="mt-1 text-sm text-gray-900">
                    {caseData.filedDate ? new Date(caseData.filedDate).toLocaleDateString() : 'N/A'}
                  </p>
                </div>
                <div>
                  <p className="text-sm font-medium text-gray-500">Hearing Date</p>
                  <p className="mt-1 text-sm text-gray-900">
                    {caseData.hearingDate ? new Date(caseData.hearingDate).toLocaleDateString() : 'N/A'}
                  </p>
                </div>
                <div>
                  <p className="text-sm font-medium text-gray-500">Trial Date</p>
                  <p className="mt-1 text-sm text-gray-900">
                    {caseData.trialDate ? new Date(caseData.trialDate).toLocaleDateString() : 'N/A'}
                  </p>
                </div>
              </div>
            </div>

            {/* AI Analysis Sections */}
            <div className="space-y-6">
              {/* Case Summary */}
              <div className="bg-white rounded-lg shadow p-6">
                <h3 className="text-xl font-semibold text-gray-700 mb-4 flex items-center">
                  <span className="mr-2">📋</span>
                  AI Case Summary
                </h3>
                <p className="text-gray-600 leading-relaxed">
                  {caseData.description || 'No detailed analysis available. Upload case documents to generate AI analysis.'}
                </p>
              </div>

              {/* AI Recommendation */}
              <div className="bg-white rounded-lg shadow p-6">
                <h3 className="text-xl font-semibold text-gray-700 mb-4 flex items-center">
                  <span className="mr-2">⚖️</span>
                  AI Recommendation
                </h3>
                <div className={`p-6 rounded-lg border ${getRecommendationClass(caseData.successProbability)}`}>
                  <p className="font-bold text-lg">{getRecommendationText(caseData.successProbability)}</p>
                  <p className="mt-2">
                    {caseData.successProbability && caseData.successProbability >= 0.7 
                      ? 'The case presents a strong potential for a successful defense. The weaknesses in the prosecution\'s evidence provide multiple avenues for reasonable doubt.'
                      : caseData.successProbability && caseData.successProbability >= 0.5
                      ? 'The outcome of a trial is uncertain. Negotiating for a reduced charge from a position of strength is advised.'
                      : 'Given the current assessment, the most advantageous strategy is to secure the best possible plea agreement.'
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
                <div className="bg-gray-50 p-4 rounded-lg hover:bg-gray-100 transition-colors cursor-pointer">
                  <p className="font-semibold text-gray-800">Crawford v. Washington (2004)</p>
                  <p className="text-sm text-gray-500 mt-1">Impacts admissibility of witness testimony and confrontation clause issues.</p>
                </div>
                <div className="bg-gray-50 p-4 rounded-lg hover:bg-gray-100 transition-colors cursor-pointer">
                  <p className="font-semibold text-gray-800">Brady v. Maryland (1963)</p>
                  <p className="text-sm text-gray-500 mt-1">Relates to prosecution\'s duty to disclose exculpatory evidence.</p>
                </div>
                <div className="bg-gray-50 p-4 rounded-lg hover:bg-gray-100 transition-colors cursor-pointer">
                  <p className="font-semibold text-gray-800">Terry v. Ohio (1968)</p>
                  <p className="text-sm text-gray-500 mt-1">Concerns the legality of investigatory stops and frisks.</p>
                </div>
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