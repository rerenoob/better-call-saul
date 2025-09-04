import React, { createContext, useReducer, useEffect } from 'react';
import { authService } from '../services/authService';
import { tokenStorage } from '../utils/tokenStorage';
import { AuthState, LoginRequest } from '../types/auth';
import { authReducer, initialState } from './authReducer';

interface AuthContextType extends AuthState {
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  register: (userData: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
  }) => Promise<void>;
  clearError: () => void;
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
          // Verify token is still valid by fetching profile
          const profile = await authService.getProfile();
          dispatch({
            type: 'LOGIN_SUCCESS',
            payload: { user: profile, token },
          });
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

      tokenStorage.setToken(response.token);
      tokenStorage.setRefreshToken(response.refreshToken);
      tokenStorage.setUser(response.user);

      dispatch({
        type: 'LOGIN_SUCCESS',
        payload: { user: response.user, token: response.token },
      });
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Login failed';
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

  const register = async (userData: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
  }) => {
    try {
      dispatch({ type: 'LOGIN_START' });
      const response = await authService.register(userData);

      tokenStorage.setToken(response.token);
      tokenStorage.setRefreshToken(response.refreshToken);
      tokenStorage.setUser(response.user);

      dispatch({
        type: 'LOGIN_SUCCESS',
        payload: { user: response.user, token: response.token },
      });
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Registration failed';
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
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

