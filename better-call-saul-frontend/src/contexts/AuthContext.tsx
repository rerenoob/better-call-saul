import React, { createContext, useReducer, useEffect } from 'react';
import { authService } from '../services/authService';
import { tokenStorage } from '../utils/tokenStorage';
import { AuthState, LoginRequest, RegisterRequest, User } from '../types/auth';
import { authReducer, initialState } from './authReducer';

// Context files legitimately export multiple values for context usage

interface AuthContextType extends AuthState {
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  register: (userData: RegisterRequest) => Promise<void>;
  clearError: () => void;
  isAdmin: boolean;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);



export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [state, dispatch] = useReducer(authReducer, initialState);

  useEffect(() => {
    const initializeAuth = async () => {
      const token = tokenStorage.getToken();
      const user = tokenStorage.getUser();

      if (token && user) {
        try {
          // For mock tokens, skip profile verification
          if (token.startsWith('mock-')) {
            dispatch({
              type: 'LOGIN_SUCCESS',
              payload: { user, token },
            });
          } else {
            // Verify token is still valid by fetching profile
            const profile = await authService.getProfile();
            const profileUser: User = {
              id: profile.id,
              email: profile.email,
              fullName: profile.fullName,
              roles: profile.roles || ['User']
            };
            dispatch({
              type: 'LOGIN_SUCCESS',
              payload: { user: profileUser, token },
            });
          }
        } catch (error) {
          tokenStorage.clear();
          dispatch({ type: 'LOGOUT' });
        }
      }
    };

    initializeAuth();
  }, []);

  const login = async (credentials: LoginRequest) => {
    try {
      dispatch({ type: 'LOGIN_START' });
      const response = await authService.login(credentials);

      const user: User = {
        id: response.userId,
        email: response.email,
        fullName: response.fullName,
        roles: response.roles
      };

      tokenStorage.setToken(response.token);
      tokenStorage.setRefreshToken(response.refreshToken);
      tokenStorage.setUser(user);

      dispatch({
        type: 'LOGIN_SUCCESS',
        payload: { user, token: response.token },
      });
    } catch (error: unknown) {
      let errorMessage = 'Login failed. Please try again.';

      // Handle axios error with response
      if (error && typeof error === 'object' && 'response' in error) {
        const axiosError = error as { response: { status: number; data?: { message?: string } } };
        const status = axiosError.response.status;

        if (status === 401) {
          errorMessage = 'Invalid email or password. Please check your credentials and try again.';
        } else if (status === 429) {
          errorMessage = 'Too many login attempts. Please wait a moment and try again.';
        } else if (status === 500) {
          errorMessage = 'Server error. Please try again later.';
        } else if (status >= 400) {
          errorMessage = axiosError.response.data?.message || 'Authentication failed. Please try again.';
        }
      } else if (error && typeof error === 'object' && 'code' in error) {
        const networkError = error as { code: string };
        if (networkError.code === 'NETWORK_ERROR' || !navigator.onLine) {
          errorMessage = 'Network error. Please check your internet connection.';
        }
      }

      dispatch({ type: 'LOGIN_FAILURE', payload: errorMessage });
      throw error;
    }
  };

  const logout = async () => {
    try {
      await authService.logout();
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      tokenStorage.clear();
      dispatch({ type: 'LOGOUT' });
    }
  };

  const register = async (userData: RegisterRequest) => {
    try {
      dispatch({ type: 'LOGIN_START' });
      const response = await authService.register(userData);

      const user: User = {
        id: response.userId,
        email: response.email,
        fullName: response.fullName,
        roles: response.roles
      };

      tokenStorage.setToken(response.token);
      tokenStorage.setRefreshToken(response.refreshToken);
      tokenStorage.setUser(user);

      dispatch({
        type: 'LOGIN_SUCCESS',
        payload: { user, token: response.token },
      });
    } catch (error: unknown) {
      let errorMessage = 'Registration failed. Please try again.';

      // Handle axios error with response
      if (error && typeof error === 'object' && 'response' in error) {
        const axiosError = error as { response: { status: number; data?: { message?: string } } };
        const status = axiosError.response.status;

        if (status === 400) {
          errorMessage = axiosError.response.data?.message || 'Invalid registration data. Please check your information.';
        } else if (status === 409) {
          errorMessage = 'An account with this email already exists. Please try logging in instead.';
        } else if (status === 429) {
          errorMessage = 'Too many registration attempts. Please wait a moment and try again.';
        } else if (status === 500) {
          errorMessage = 'Server error. Please try again later.';
        } else if (status >= 400) {
          errorMessage = axiosError.response.data?.message || 'Registration failed. Please try again.';
        }
      } else if (error && typeof error === 'object' && 'code' in error) {
        const networkError = error as { code: string };
        if (networkError.code === 'NETWORK_ERROR' || !navigator.onLine) {
          errorMessage = 'Network error. Please check your internet connection.';
        }
      }

      dispatch({ type: 'LOGIN_FAILURE', payload: errorMessage });
      throw error;
    }
  };

  const clearError = () => {
    dispatch({ type: 'CLEAR_ERROR' });
  };

  const value: AuthContextType = {
    ...state,
    login,
    logout,
    register,
    clearError,
    isAdmin: state.user?.roles.includes('Admin') || false,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

