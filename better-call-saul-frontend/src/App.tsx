import { BrowserRouter as Router, Routes, Route, Navigate, useParams, useNavigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { LoginForm } from './components/auth/LoginForm';
import { useAuth } from './hooks/useAuth';

// Create a client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

function Dashboard() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const mockCases = [
    {
      id: 1,
      title: "State v. Johnson",
      client: "Michael Johnson", 
      priority: "High Priority",
      priorityColor: "bg-green-100 text-green-800",
      aiPrediction: 82,
      predictionColor: "bg-green-500"
    },
    {
      id: 2,
      title: "State v. Chen",
      client: "Wei Chen",
      priority: "Medium Priority", 
      priorityColor: "bg-yellow-100 text-yellow-800",
      aiPrediction: 55,
      predictionColor: "bg-yellow-500"
    },
    {
      id: 3,
      title: "State v. Rodriguez",
      client: "Maria Rodriguez",
      priority: "Low Priority",
      priorityColor: "bg-red-100 text-red-800", 
      aiPrediction: 28,
      predictionColor: "bg-red-500"
    }
  ];

  return (
    <div className="min-h-screen bg-gray-50 flex">
      {/* Dark Sidebar */}
      <div className="w-64 bg-slate-800 text-white flex flex-col">
        {/* Logo/Brand */}
        <div className="p-6 border-b border-slate-700">
          <h1 className="text-xl font-bold flex items-center">
            ⚖️ BCS AI
          </h1>
        </div>

        {/* Navigation */}
        <nav className="flex-1 p-4">
          <div className="space-y-2">
            <a href="#" className="flex items-center px-4 py-3 text-white bg-slate-700 rounded-lg">
              📊 Dashboard
            </a>
            <a href="#" className="flex items-center px-4 py-3 text-slate-300 hover:text-white hover:bg-slate-700 rounded-lg">
              📄 Add New Case
            </a>
          </div>
        </nav>

        {/* User Profile */}
        <div className="p-4 border-t border-slate-700">
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 bg-blue-600 rounded-full flex items-center justify-center text-white font-bold">
              PD
            </div>
            <div>
              <p className="text-sm font-medium">{user?.fullName?.split(' ')[0]} Rivera</p>
              <p className="text-xs text-slate-400">Public Defender</p>
            </div>
          </div>
          <button 
            onClick={() => logout()}
            className="mt-3 w-full text-left text-xs text-slate-400 hover:text-white flex items-center"
          >
            🚪 Logout
          </button>
        </div>
      </div>

      {/* Main Content */}
      <div className="flex-1 overflow-hidden">
        {/* Header */}
        <header className="bg-white shadow-sm p-6">
          <h1 className="text-2xl font-bold text-gray-900">Case Dashboard</h1>
        </header>

        {/* Content */}
        <main className="p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {mockCases.map((case_) => (
              <div key={case_.id} className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <div className="flex justify-between items-start mb-4">
                  <h3 className="text-lg font-semibold text-gray-900">{case_.title}</h3>
                  <span className={`px-2 py-1 text-xs font-medium rounded ${case_.priorityColor}`}>
                    {case_.priority}
                  </span>
                </div>
                
                <p className="text-sm text-gray-600 mb-4">Client: {case_.client}</p>
                
                <div className="mb-4">
                  <div className="flex justify-between items-center mb-2">
                    <span className="text-sm font-medium text-gray-700">AI Success Prediction</span>
                    <span className="text-sm font-bold">{case_.aiPrediction}%</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-3">
                    <div 
                      className={`h-3 rounded-full ${case_.predictionColor}`}
                      style={{width: `${case_.aiPrediction}%`}}
                    ></div>
                  </div>
                </div>
                
                <button 
                  onClick={() => navigate(`/case/${case_.id}`)}
                  className="w-full bg-blue-600 text-white py-2 px-4 rounded-lg hover:bg-blue-700 text-sm font-medium"
                >
                  View Analysis
                </button>
              </div>
            ))}
          </div>
        </main>
      </div>
    </div>
  );
}

function CaseDetail() {
  const { id } = useParams();
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const mockCaseData = {
    1: {
      title: "State v. Johnson",
      client: "Michael Johnson",
      aiPrediction: 82,
      summary: "The case against Michael Johnson relies heavily on circumstantial evidence and a single, uncorroborated witness testimony. The witness has a history of credibility issues. Physical evidence linking Mr. Johnson to the scene is inconclusive. The prosecution's timeline has significant inconsistencies that can be challenged. Motion to suppress the witness testimony is likely to be granted.",
      recommendation: "Proceed to Trial",
      recommendationText: "The case presents a strong potential for a successful defense. The weaknesses in the prosecution's evidence provide multiple avenues for reasonable doubt. A plea deal is not recommended at this stage as it undervalues the defensive position.",
      relevantCases: [
        { name: "Crawford v. Washington (2004)", description: "Impacts admissibility of witness testimony." },
        { name: "Brady v. Maryland (1963)", description: "Relates to disclosure of exculpatory evidence." },
        { name: "Terry v. Ohio (1968)", description: "Concerns the legality of the initial stop." }
      ]
    },
    2: {
      title: "State v. Chen",
      client: "Wei Chen", 
      aiPrediction: 55,
      summary: "The prosecution has presented a moderate case with mixed evidence quality. Key witness testimonies are consistent but lack corroborating physical evidence. Chain of custody issues exist with some digital evidence. The defendant has no prior criminal history which works in favor of the defense.",
      recommendation: "Consider Plea Negotiation",
      recommendationText: "While there are defendable aspects to this case, the consistent witness testimony presents challenges. A strategic plea negotiation might be beneficial to avoid potential conviction risks.",
      relevantCases: [
        { name: "Miranda v. Arizona (1966)", description: "Rights during interrogation apply." },
        { name: "Mapp v. Ohio (1961)", description: "Exclusionary rule for illegally obtained evidence." }
      ]
    },
    3: {
      title: "State v. Rodriguez", 
      client: "Maria Rodriguez",
      aiPrediction: 28,
      summary: "The prosecution presents a strong case with substantial physical evidence and multiple credible witness testimonies. The defendant's alibi has been contradicted by surveillance footage. DNA evidence places the defendant at the scene during the relevant time period.",
      recommendation: "Negotiate Plea Agreement",
      recommendationText: "Given the strength of the prosecution's evidence, pursuing a plea agreement would likely result in a more favorable outcome than proceeding to trial. Focus should be on mitigation factors and reduced charges.",
      relevantCases: [
        { name: "Gideon v. Wainwright (1963)", description: "Right to effective counsel in criminal proceedings." },
        { name: "Strickland v. Washington (1984)", description: "Standard for effective assistance of counsel." }
      ]
    }
  };

  const caseData = mockCaseData[id as keyof typeof mockCaseData];

  if (!caseData) {
    return <div>Case not found</div>;
  }

  return (
    <div className="min-h-screen bg-gray-50 flex">
      {/* Dark Sidebar */}
      <div className="w-64 bg-slate-800 text-white flex flex-col">
        {/* Logo/Brand */}
        <div className="p-6 border-b border-slate-700">
          <h1 className="text-xl font-bold flex items-center">
            ⚖️ BCS AI
          </h1>
        </div>

        {/* Navigation */}
        <nav className="flex-1 p-4">
          <div className="space-y-2">
            <a href="#" onClick={() => navigate('/dashboard')} className="flex items-center px-4 py-3 text-slate-300 hover:text-white hover:bg-slate-700 rounded-lg">
              📊 Dashboard
            </a>
            <a href="#" className="flex items-center px-4 py-3 text-slate-300 hover:text-white hover:bg-slate-700 rounded-lg">
              📄 Add New Case
            </a>
          </div>
        </nav>

        {/* User Profile */}
        <div className="p-4 border-t border-slate-700">
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 bg-blue-600 rounded-full flex items-center justify-center text-white font-bold">
              PD
            </div>
            <div>
              <p className="text-sm font-medium">{user?.fullName?.split(' ')[0]} Rivera</p>
              <p className="text-xs text-slate-400">Public Defender</p>
            </div>
          </div>
          <button 
            onClick={() => logout()}
            className="mt-3 w-full text-left text-xs text-slate-400 hover:text-white flex items-center"
          >
            🚪 Logout
          </button>
        </div>
      </div>

      {/* Main Content */}
      <div className="flex-1 overflow-hidden">
        {/* Header */}
        <header className="bg-white shadow-sm p-6">
          <div className="flex items-center space-x-4">
            <button 
              onClick={() => navigate('/dashboard')}
              className="text-blue-600 hover:text-blue-700 flex items-center space-x-2"
            >
              ← Back to Dashboard
            </button>
          </div>
          <div className="flex justify-between items-start mt-4">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">{caseData.title}</h1>
              <p className="text-gray-600">Client: {caseData.client}</p>
            </div>
            <div className="text-right">
              <p className="text-sm text-gray-500">AI Success Prediction</p>
              <p className="text-3xl font-bold text-green-600">{caseData.aiPrediction}%</p>
            </div>
          </div>
        </header>

        {/* Content */}
        <main className="p-6">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Left Column - Main Content */}
            <div className="lg:col-span-2 space-y-6">
              {/* AI Case Summary */}
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
                  📄 AI Case Summary
                </h2>
                <p className="text-gray-700 leading-relaxed">
                  {caseData.summary}
                </p>
              </div>

              {/* AI Recommendation */}
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
                  🔧 AI Recommendation
                </h2>
                <div className="bg-blue-50 border-l-4 border-blue-500 p-4 rounded">
                  <h3 className="font-semibold text-blue-900 mb-2">{caseData.recommendation}</h3>
                  <p className="text-blue-800 leading-relaxed">
                    {caseData.recommendationText}
                  </p>
                </div>
              </div>
            </div>

            {/* Right Column - Sidebar */}
            <div className="space-y-6">
              {/* Relevant Case Law */}
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
                  📚 Relevant Case Law
                </h2>
                <div className="space-y-4">
                  {caseData.relevantCases.map((case_, index) => (
                    <div key={index} className="border-b border-gray-200 pb-3 last:border-b-0">
                      <h4 className="font-medium text-gray-900">{case_.name}</h4>
                      <p className="text-sm text-gray-600 mt-1">{case_.description}</p>
                    </div>
                  ))}
                </div>
              </div>

              {/* Ask the AI */}
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
                  🤖 Ask the AI
                </h2>
                <div className="space-y-3">
                  <textarea
                    placeholder="Ask a follow-up question..."
                    className="w-full p-3 border border-gray-300 rounded-lg resize-none h-20 text-sm"
                  />
                  <button className="w-full bg-blue-600 text-white py-2 px-4 rounded-lg hover:bg-blue-700 text-sm font-medium">
                    Send
                  </button>
                </div>
              </div>
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}

function LoginPage() {
  const handleLoginSuccess = () => {
    window.location.href = '/dashboard';
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <LoginForm onSuccess={handleLoginSuccess} />
    </div>
  );
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <Router>
          <Routes>
            <Route path="/login" element={
              <ProtectedRoute requireAuth={false}>
                <LoginPage />
              </ProtectedRoute>
            } />
            <Route path="/dashboard" element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            } />
            <Route path="/case/:id" element={
              <ProtectedRoute>
                <CaseDetail />
              </ProtectedRoute>
            } />
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </Router>
      </AuthProvider>
    </QueryClientProvider>
  );
}

export default App;