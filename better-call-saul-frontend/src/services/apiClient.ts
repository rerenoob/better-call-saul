import axios from 'axios';
import { tokenStorage } from '../utils/tokenStorage';

const GATEWAY_BASE_URL = import.meta.env.VITE_API_BASE_URL || import.meta.env.VITE_GATEWAY_BASE_URL || 'http://localhost:7191';

export const apiClient = axios.create({
  baseURL: `${GATEWAY_BASE_URL}`,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = tokenStorage.getToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = tokenStorage.getRefreshToken();
        if (refreshToken) {
          const response = await axios.post(`${GATEWAY_BASE_URL}/api/auth/refresh`, {
            refreshToken,
          });

          const { token, refreshToken: newRefreshToken } = response.data;
          tokenStorage.setToken(token);
          tokenStorage.setRefreshToken(newRefreshToken);

          originalRequest.headers.Authorization = `Bearer ${token}`;
          return apiClient(originalRequest);
        }
      } catch (refreshError) {
        tokenStorage.clear();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);