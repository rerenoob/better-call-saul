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

