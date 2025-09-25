import { BrowserRouter as Router, Routes, Route, Navigate, useNavigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { LoginForm } from './components/auth/LoginForm';
import { AdminLoginForm } from './components/auth/AdminLoginForm';
import { RegisterForm } from './components/auth/RegisterForm';
import { CaseDetail } from './components/CaseDetail';
import { Dashboard } from './components/Dashboard';
import { CaseUpload } from './components/CaseUpload';
import { AdminLayout } from './components/admin/AdminLayout';
import { AdminDashboard } from './pages/admin/AdminDashboard';
import { UserManagement } from './pages/admin/UserManagement';
import { SystemHealth } from './pages/admin/SystemHealth';
import { AuditLogs } from './pages/admin/AuditLogs';
import { RegistrationCodes } from './pages/admin/RegistrationCodes';

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

  const handleSwitchToAdminLogin = () => {
    navigate('/admin-login');
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-gray-100 p-4 sm:p-6 lg:p-8">
      <div className="w-full max-w-sm sm:max-w-md transform transition-all duration-300 hover:scale-[1.01]">
        <LoginForm 
          onSuccess={handleLoginSuccess} 
          onSwitchToRegister={handleSwitchToRegister}
          onSwitchToAdminLogin={handleSwitchToAdminLogin}
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

function AdminLoginPage() {
  const navigate = useNavigate();

  const handleLoginSuccess = () => {
    navigate('/admin/dashboard');
  };

  const handleSwitchToUserLogin = () => {
    navigate('/login');
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-red-50 to-gray-100 p-4 sm:p-6 lg:p-8">
      <div className="w-full max-w-sm sm:max-w-md transform transition-all duration-300 hover:scale-[1.01]">
        <AdminLoginForm 
          onSuccess={handleLoginSuccess} 
          onSwitchToUserLogin={handleSwitchToUserLogin}
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
            <Route path="/admin-login" element={
              <ProtectedRoute requireAuth={false}>
                <AdminLoginPage />
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
            <Route path="/admin" element={
              <ProtectedRoute requireAdmin>
                <AdminLayout />
              </ProtectedRoute>
            }>
              <Route index element={<Navigate to="dashboard" replace />} />
              <Route path="dashboard" element={<AdminDashboard />} />
              <Route path="users" element={<UserManagement />} />
              <Route path="health" element={<SystemHealth />} />
              <Route path="audit-logs" element={<AuditLogs />} />
              <Route path="registration-codes" element={<RegistrationCodes />} />
            </Route>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </Router>
      </AuthProvider>
    </QueryClientProvider>
  );
}

export default App;