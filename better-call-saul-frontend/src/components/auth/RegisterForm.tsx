import React, { useState } from 'react';
import { useAuth } from '../../hooks/useAuth';
import { RegisterRequest } from '../../types/auth';

interface RegisterFormProps {
  onSuccess?: () => void;
  onSwitchToLogin?: () => void;
}

export const RegisterForm: React.FC<RegisterFormProps> = ({ onSuccess, onSwitchToLogin }) => {
  const [formData, setFormData] = useState<RegisterRequest>({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    barNumber: '',
    lawFirm: '',
    registrationCode: '',
  });
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const { register, error, clearError } = useAuth();

  const validateForm = () => {
    const errors: Record<string, string> = {};
    
    // Email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!formData.email) {
      errors.email = 'Email is required';
    } else if (!emailRegex.test(formData.email)) {
      errors.email = 'Please enter a valid email address';
    }
    
    // Password validation
    if (!formData.password) {
      errors.password = 'Password is required';
    } else if (formData.password.length < 8) {
      errors.password = 'Password must be at least 8 characters long';
    } else if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d])/.test(formData.password)) {
      errors.password = 'Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character';
    }
    
    // Confirm password validation
    if (!confirmPassword) {
      errors.confirmPassword = 'Please confirm your password';
    } else if (formData.password !== confirmPassword) {
      errors.confirmPassword = 'Passwords do not match';
    }
    
    // Required fields validation
    if (!formData.firstName.trim()) {
      errors.firstName = 'First name is required';
    }
    if (!formData.lastName.trim()) {
      errors.lastName = 'Last name is required';
    }
    if (!formData.registrationCode.trim()) {
      errors.registrationCode = 'Registration code is required';
    }
    
    return errors;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const validationErrors = validateForm();
    if (Object.keys(validationErrors).length > 0) {
      setFieldErrors(validationErrors);
      return;
    }

    setIsLoading(true);
    setFieldErrors({});
    clearError();

    try {
      await register(formData);
      onSuccess?.();
    } catch (error: unknown) {
      console.error('Registration error:', error);
      
      // Handle specific error cases
      if (error && typeof error === 'object' && 'response' in error) {
        const axiosError = error as { response?: { status?: number; data?: unknown } };
        if (axiosError.response?.status === 400) {
          const errorData = axiosError.response.data;
          // Type guard to check if errorData has the expected structure
          if (errorData && typeof errorData === 'object') {
            const data = errorData as { message?: string; errors?: Record<string, unknown> };
            if (data.message?.includes('registration code')) {
              setFieldErrors({ registrationCode: 'Invalid registration code. Please check the code and try again.' });
            } else if (data.message?.includes('email')) {
              setFieldErrors({ email: 'This email is already registered. Please use a different email or try logging in.' });
            } else {
              // Handle validation errors from server
              if (data.errors) {
                const serverErrors: Record<string, string> = {};
                Object.keys(data.errors).forEach(key => {
                  const fieldName = key.charAt(0).toLowerCase() + key.slice(1);
                  const errorValue = data.errors![key];
                  if (Array.isArray(errorValue)) {
                    serverErrors[fieldName] = String(errorValue[0] || '');
                  } else {
                    serverErrors[fieldName] = String(errorValue || '');
                  }
                });
                setFieldErrors(serverErrors);
              }
            }
          }
        } else if (axiosError.response?.status === 409) {
          setFieldErrors({ email: 'An account with this email already exists. Please use a different email or try logging in.' });
        } else if (axiosError.response?.status === 422) {
          setFieldErrors({ registrationCode: 'This registration code has already been used or is no longer valid.' });
        }
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    
    // Clear field-specific error when user starts typing
    if (fieldErrors[name]) {
      setFieldErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors[name];
        return newErrors;
      });
    }
    
    if (name === 'confirmPassword') {
      setConfirmPassword(value);
      // Clear confirm password error when user starts typing
      if (fieldErrors.confirmPassword) {
        setFieldErrors(prev => {
          const newErrors = { ...prev };
          delete newErrors.confirmPassword;
          return newErrors;
        });
      }
    } else {
      setFormData(prev => ({
        ...prev,
        [name]: value,
      }));
    }
  };

  const passwordsMatch = formData.password === confirmPassword;
  const isConfirmPasswordEmpty = confirmPassword === '';
  
  const getErrorMessage = (error: string): string => {
    // Map generic server errors to user-friendly messages
    if (error.includes('Registration failed')) {
      return 'Unable to create your account. Please check your information and try again.';
    }
    if (error.includes('Network Error') || error.includes('timeout')) {
      return 'Connection problem. Please check your internet connection and try again.';
    }
    if (error.includes('500')) {
      return 'Server error. Please try again in a few moments.';
    }
    return error;
  };

  return (
    <div className="w-full max-w-2xl mx-auto bg-white rounded-xl shadow-lg p-4 sm:p-6 md:p-8 border border-gray-100">
      <h2 className="text-xl sm:text-2xl font-bold text-center mb-4 sm:mb-6 text-gray-800">
        📝 Register for Better Call Saul
      </h2>
      
      {error && Object.keys(fieldErrors).length === 0 && (
        <div className="bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded-lg mb-4 flex items-start gap-3">
          <span className="text-red-500 text-lg">⚠️</span>
          <div>
            <p className="font-medium text-sm">Registration Error</p>
            <p className="text-sm mt-1">{getErrorMessage(error)}</p>
          </div>
        </div>
      )}


      <form onSubmit={handleSubmit} className="space-y-3 sm:space-y-4">
        {/* Registration Code - Most Important Field */}
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <label htmlFor="registrationCode" className="block text-sm font-semibold text-blue-800 mb-2">
            🎫 Registration Code *
          </label>
          <input
            type="text"
            id="registrationCode"
            name="registrationCode"
            value={formData.registrationCode}
            onChange={handleChange}
            required
            className={`block w-full px-4 py-3 text-base border-2 rounded-lg shadow-sm focus:outline-none focus:ring-2 transition-colors duration-200 font-mono tracking-wider ${
              fieldErrors.registrationCode
                ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
                : 'border-blue-300 focus:ring-blue-500 focus:border-blue-500'
            }`}
            placeholder="Enter your registration code"
            style={{ textTransform: 'uppercase' }}
          />
          {fieldErrors.registrationCode && (
            <p className="text-red-600 text-xs mt-1 flex items-center gap-1">
              <span>❌</span> {fieldErrors.registrationCode}
            </p>
          )}
          <p className="text-xs text-blue-600 mt-1">
            You need a valid registration code to create an account. Contact your administrator to obtain one.
          </p>
        </div>

        {/* Personal Information */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 sm:gap-4">
          <div>
            <label htmlFor="firstName" className="block text-xs sm:text-sm font-medium text-gray-700">
              First Name *
            </label>
            <input
              type="text"
              id="firstName"
              name="firstName"
              value={formData.firstName}
              onChange={handleChange}
              required
              className={`mt-1 block w-full px-4 py-3 text-base border rounded-lg shadow-sm focus:outline-none focus:ring-2 transition-colors duration-200 ${
                fieldErrors.firstName
                  ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
                  : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500'
              }`}
              placeholder="Enter your first name"
            />
            {fieldErrors.firstName && (
              <p className="text-red-600 text-xs mt-1 flex items-center gap-1">
                <span>❌</span> {fieldErrors.firstName}
              </p>
            )}
          </div>

          <div>
            <label htmlFor="lastName" className="block text-xs sm:text-sm font-medium text-gray-700">
              Last Name *
            </label>
            <input
              type="text"
              id="lastName"
              name="lastName"
              value={formData.lastName}
              onChange={handleChange}
              required
              className={`mt-1 block w-full px-4 py-3 text-base border rounded-lg shadow-sm focus:outline-none focus:ring-2 transition-colors duration-200 ${
                fieldErrors.lastName
                  ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
                  : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500'
              }`}
              placeholder="Enter your last name"
            />
            {fieldErrors.lastName && (
              <p className="text-red-600 text-xs mt-1 flex items-center gap-1">
                <span>❌</span> {fieldErrors.lastName}
              </p>
            )}
          </div>
        </div>

        {/* Email */}
        <div>
          <label htmlFor="email" className="block text-xs sm:text-sm font-medium text-gray-700">
            Email *
          </label>
          <input
            type="email"
            id="email"
            name="email"
            value={formData.email}
            onChange={handleChange}
            required
            className={`mt-1 block w-full px-4 py-3 text-base border rounded-lg shadow-sm focus:outline-none focus:ring-2 transition-colors duration-200 ${
              fieldErrors.email
                ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
                : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500'
            }`}
            placeholder="Enter your email address"
            autoComplete="email"
          />
          {fieldErrors.email && (
            <p className="text-red-600 text-xs mt-1 flex items-center gap-1">
              <span>❌</span> {fieldErrors.email}
            </p>
          )}
        </div>

        {/* Password Fields */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 sm:gap-4">
          <div>
            <label htmlFor="password" className="block text-xs sm:text-sm font-medium text-gray-700">
              Password *
            </label>
            <input
              type="password"
              id="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              required
              className={`mt-1 block w-full px-4 py-3 text-base border rounded-lg shadow-sm focus:outline-none focus:ring-2 transition-colors duration-200 ${
                fieldErrors.password
                  ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
                  : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500'
              }`}
              placeholder="Create a password"
              autoComplete="new-password"
            />
            {fieldErrors.password && (
              <p className="text-red-600 text-xs mt-1 flex items-center gap-1">
                <span>❌</span> {fieldErrors.password}
              </p>
            )}
            {formData.password && !fieldErrors.password && (
              <div className="mt-2 space-y-1">
                <div className="flex items-center gap-2 text-xs">
                  <span className={formData.password.length >= 8 ? 'text-green-600' : 'text-gray-400'}>
                    {formData.password.length >= 8 ? '✅' : '⭕'}
                  </span>
                  <span className={formData.password.length >= 8 ? 'text-green-600' : 'text-gray-500'}>
                    At least 8 characters
                  </span>
                </div>
                <div className="flex items-center gap-2 text-xs">
                  <span className={/(?=.*[a-z])(?=.*[A-Z])/.test(formData.password) ? 'text-green-600' : 'text-gray-400'}>
                    {/(?=.*[a-z])(?=.*[A-Z])/.test(formData.password) ? '✅' : '⭕'}
                  </span>
                  <span className={/(?=.*[a-z])(?=.*[A-Z])/.test(formData.password) ? 'text-green-600' : 'text-gray-500'}>
                    Both uppercase and lowercase letters
                  </span>
                </div>
                <div className="flex items-center gap-2 text-xs">
                  <span className={/(?=.*\d)/.test(formData.password) ? 'text-green-600' : 'text-gray-400'}>
                    {/(?=.*\d)/.test(formData.password) ? '✅' : '⭕'}
                  </span>
                  <span className={/(?=.*\d)/.test(formData.password) ? 'text-green-600' : 'text-gray-500'}>
                    At least one number
                  </span>
                </div>
                <div className="flex items-center gap-2 text-xs">
                  <span className={/(?=.*[^a-zA-Z\d])/.test(formData.password) ? 'text-green-600' : 'text-gray-400'}>
                    {/(?=.*[^a-zA-Z\d])/.test(formData.password) ? '✅' : '⭕'}
                  </span>
                  <span className={/(?=.*[^a-zA-Z\d])/.test(formData.password) ? 'text-green-600' : 'text-gray-500'}>
                    At least one special character
                  </span>
                </div>
              </div>
            )}
          </div>

          <div>
            <label htmlFor="confirmPassword" className="block text-xs sm:text-sm font-medium text-gray-700">
              Confirm Password *
            </label>
            <input
              type="password"
              id="confirmPassword"
              name="confirmPassword"
              value={confirmPassword}
              onChange={handleChange}
              required
              className={`mt-1 block w-full px-4 py-3 text-base border rounded-lg shadow-sm focus:outline-none focus:ring-2 transition-colors duration-200 ${
                fieldErrors.confirmPassword
                  ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
                  : !isConfirmPasswordEmpty && passwordsMatch
                  ? 'border-green-300 focus:ring-green-500 focus:border-green-500'
                  : !isConfirmPasswordEmpty && !passwordsMatch
                  ? 'border-yellow-300 focus:ring-yellow-500 focus:border-yellow-500'
                  : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500'
              }`}
              placeholder="Confirm your password"
              autoComplete="new-password"
            />
            {fieldErrors.confirmPassword && (
              <p className="text-red-600 text-xs mt-1 flex items-center gap-1">
                <span>❌</span> {fieldErrors.confirmPassword}
              </p>
            )}
            {!fieldErrors.confirmPassword && !isConfirmPasswordEmpty && passwordsMatch && (
              <p className="text-green-600 text-xs mt-1 flex items-center gap-1">
                <span>✅</span> Passwords match
              </p>
            )}
            {!fieldErrors.confirmPassword && !isConfirmPasswordEmpty && !passwordsMatch && (
              <p className="text-yellow-600 text-xs mt-1 flex items-center gap-1">
                <span>⚠️</span> Passwords do not match
              </p>
            )}
          </div>
        </div>

        {/* Professional Information */}
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
          <h3 className="text-sm font-medium text-gray-700 mb-3">Professional Information (Optional)</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3 sm:gap-4">
            <div>
              <label htmlFor="barNumber" className="block text-xs sm:text-sm font-medium text-gray-700">
                Bar Number
              </label>
              <input
                type="text"
                id="barNumber"
                name="barNumber"
                value={formData.barNumber}
                onChange={handleChange}
                className="mt-1 block w-full px-4 py-3 text-base border border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors duration-200"
                placeholder="Your bar number"
              />
            </div>

            <div>
              <label htmlFor="lawFirm" className="block text-xs sm:text-sm font-medium text-gray-700">
                Law Firm / Organization
              </label>
              <input
                type="text"
                id="lawFirm"
                name="lawFirm"
                value={formData.lawFirm}
                onChange={handleChange}
                className="mt-1 block w-full px-4 py-3 text-base border border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors duration-200"
                placeholder="Your law firm or organization"
              />
            </div>
          </div>
        </div>

        <button
          type="submit"
          disabled={isLoading}
          className="w-full bg-green-600 text-white py-3 px-4 rounded-lg hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed text-base font-medium transition-colors duration-200 transform hover:scale-[1.02] active:scale-[0.98]"
        >
          {isLoading ? '📝 Creating Account...' : '🚀 Create Account'}
        </button>
      </form>

      {onSwitchToLogin && (
        <div className="mt-3 sm:mt-4 text-center">
          <p className="text-xs sm:text-sm text-gray-600">
            Already have an account?{' '}
            <button
              type="button"
              onClick={onSwitchToLogin}
              className="text-blue-600 hover:text-blue-800 font-medium text-xs sm:text-sm"
            >
              Login here
            </button>
          </p>
        </div>
      )}
    </div>
  );
};