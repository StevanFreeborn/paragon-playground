import { api } from './api';

export interface UserResponse {
  id: string;
  email: string;
  displayName: string;
  organizationId: string;
  organizationName: string;
  organizationSlug: string;
}

export async function login(email: string, password: string): Promise<UserResponse> {
  return api<UserResponse>('/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
}

export async function logout(): Promise<void> {
  await api<void>('/auth/logout', { method: 'POST' });
}

export async function me(): Promise<UserResponse> {
  return api<UserResponse>('/auth/me');
}
