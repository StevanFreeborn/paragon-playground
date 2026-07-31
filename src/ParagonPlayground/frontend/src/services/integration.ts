import { api } from './api';

export interface ParagonTokenResponse {
  paragonJwt: string;
  projectId: string;
}

export interface IntegrationConfig {
  id: string;
  organizationId: string;
  connectionMode: string;
  sharePointSiteUrl: string | null;
  sharePointSiteId: string | null;
  sharePointFolderPath: string | null;
  updatedAt: string;
}

export interface IntegrationConfigRequest {
  connectionMode: string;
  sharePointSiteUrl: string | null;
  sharePointFolderPath: string | null;
}

export interface CredentialResponse {
  id: string;
  credentialId: string;
  integrationType: string;
  connectedAt: string;
}

export interface CredentialRequest {
  credentialId: string;
  integrationType: string;
}

export async function getParagonToken(): Promise<ParagonTokenResponse> {
  return api<ParagonTokenResponse>('/paragon/token');
}

export async function getConfig(): Promise<IntegrationConfig> {
  return api<IntegrationConfig>('/integration/config');
}

export async function updateConfig(config: IntegrationConfigRequest): Promise<IntegrationConfig> {
  return api<IntegrationConfig>('/integration/config', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(config),
  });
}

export async function getCredentials(): Promise<CredentialResponse[]> {
  return api<CredentialResponse[]>('/integration/credentials');
}

export async function getOrgCredentials(): Promise<CredentialResponse[]> {
  return api<CredentialResponse[]>('/integration/credentials/org');
}

export async function saveCredential(req: CredentialRequest): Promise<CredentialResponse> {
  return api<CredentialResponse>('/integration/credentials', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  });
}

export async function deleteCredential(credentialId: string): Promise<void> {
  await api<void>(`/integration/credentials/${encodeURIComponent(credentialId)}`, {
    method: 'DELETE',
  });
}

export async function purgeOrgCredentials(): Promise<void> {
  await api<void>('/integration/credentials/org', { method: 'DELETE' });
}
