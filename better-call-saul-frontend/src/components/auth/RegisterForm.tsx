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
  const { register, error, clearError } = useAuth();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (formData.password !== confirmPassword) {
      return;
    }

    setIsLoading(true);
    clearError();

    try {
      await register(formData);
      onSuccess?.();
    } catch (error) {
      console.error('Registration error:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    if (name === 'confirmPassword') {
      setConfirmPassword(value);
    } else {
      setFormData(prev => ({
        ...prev,
        [name]: value,
      }));
    }
  };

  const passwordsMatch = formData.password === confirmPassword;
  const isPasswordEmpty = formData.password === '';
  const isConfirmPasswordEmpty = confirmPassword === '';

  return (
    <div className="w-full max-w-2xl mx-auto bg-white rounded-xl shadow-lg p-4 sm:p-6 md:p-8 border border-gray-100">
      <h2 className="text-xl sm:text-2xl font-bold text-center mb-4 sm:mb-6 text-gray-800">
        📝 Register for Better Call Saul
      </h2>
      
      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-3 py-2 sm:px-4 sm:py-3 rounded mb-3 sm:mb-4 text-xs sm:text-sm">
          {error}
        </div>
      )}

      {!isPasswordEmpty && !isConfirmPasswordEmpty && !passwordsMatch && (
        <div className="bg-yellow-100 border border-yellow-400 text-yellow-700 px-3 py-2 sm:px-4 sm:py-3 rounded mb-3 sm:mb-4 text-xs sm:text-sm">
          Passwords do not match
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
            className="block w-full px-4 py-3 text-base border-2 border-blue-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors duration-200 font-mono tracking-wider"
            placeholder="Enter your registration code"
            style={{ textTransform: 'uppercase' }}
          />
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
              className="mt-1 block w-full px-4 py-3 text-base border border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors duration-200"
              placeholder="Enter your first name"
            />
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
              className="mt-1 block w-full px-4 py-3 text-base border border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors duration-200"
              placeholder="Enter your last name"
            />
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
            className="mt-1 block w-full px-4 py-3 text-base border border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors duration-200"
            placeholder="Enter your email address"
            autoComplete="email"
          />
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
              className="mt-1 block w-full px-4 py-3 text-base border border-gray-300 rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors duration-200"
              placeholder="Create a password"
              autoComplete="new-password"
            />
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
                !isConfirmPasswordEmpty && passwordsMatch
                  ? 'border-green-300 focus:ring-green-500 focus:border-green-500'
                  : !isConfirmPasswordEmpty && !passwordsMatch
                  ? 'border-red-300 focus:ring-red-500 focus:border-red-500'
                  : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500'
              }`}
              placeholder="Confirm your password"
              autoComplete="new-password"
            />
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
          disabled={isLoading || !passwordsMatch || isPasswordEmpty}
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