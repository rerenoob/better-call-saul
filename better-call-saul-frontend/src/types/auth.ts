export interface User {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  barNumber?: string;
  lawFirm?: string;
  registrationCode: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiration: string;
  userId: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}
