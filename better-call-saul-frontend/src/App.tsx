import { BrowserRouter as Router, Routes, Route, Navigate, useNavigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { LoginForm } from './components/auth/LoginForm';
import { RegisterForm } from './components/auth/RegisterForm';
import { CaseDetail } from './components/CaseDetail';
import { Dashboard } from './components/Dashboard';
import { CaseUpload } from './components/CaseUpload';

// Create a client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});


function LoginPage() {
  const navigate = useNavigate();

  const handleLoginSuccess = () => {
    navigate('/dashboard');
  };

  const handleSwitchToRegister = () => {
    navigate('/register');
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-gray-100 p-4 sm:p-6 lg:p-8">
      <div className="w-full max-w-sm sm:max-w-md transform transition-all duration-300 hover:scale-[1.01]">
        <LoginForm 
          onSuccess={handleLoginSuccess} 
          onSwitchToRegister={handleSwitchToRegister}
        />
      </div>
    </div>
  );
}

function RegisterPage() {
  const navigate = useNavigate();

  const handleRegisterSuccess = () => {
    navigate('/dashboard');
  };

  const handleSwitchToLogin = () => {
    navigate('/login');
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-green-50 to-blue-100 p-4 sm:p-6 lg:p-8">
      <div className="w-full max-w-2xl transform transition-all duration-300 hover:scale-[1.01]">
        <RegisterForm 
          onSuccess={handleRegisterSuccess} 
          onSwitchToLogin={handleSwitchToLogin}
        />
      </div>
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
            <Route path="/register" element={
              <ProtectedRoute requireAuth={false}>
                <RegisterPage />
              </ProtectedRoute>
            } />
            <Route path="/dashboard" element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            } />
            <Route path="/cases/:id" element={
              <ProtectedRoute>
                <CaseDetail />
              </ProtectedRoute>
            } />
            <Route path="/cases/new" element={
              <ProtectedRoute>
                <CaseUpload />
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