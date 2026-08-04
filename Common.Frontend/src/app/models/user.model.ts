export interface User {
  id?: string;
  username: string;
  passwordHash?: string;
  role: string;
  email?: string;
  createdAt?: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  password: string;
  role?: string;
  email?: string;
}

export interface UserCreateRequest {
  username: string;
  password: string;
  role: string;
  email?: string;
}

export interface UserUpdateRequest {
  password?: string;
  role?: string;
  email?: string;
}

export interface UserResponse {
  id: string;
  username: string;
  role: string;
  email?: string;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  role: string;
  username: string;
  userId: string;
}
