import { api } from './api';

export interface StorageItem {
  id: string;
  name: string;
  isFolder: boolean;
  parentId: string | null;
  contentType: string | null;
  fileSize: number;
  sharePointWebUrl: string | null;
  createdByUserId: string;
  createdByDisplayName: string;
  createdAt: string;
}

export interface CreateFolderRequest {
  name: string;
  parentId: string | null;
}

export interface DownloadResponse {
  sharePointUrl: string | null;
  proxyUrl: string | null;
}

export async function getItems(parentId?: string | null): Promise<StorageItem[]> {
  const params = parentId ? `?parentId=${encodeURIComponent(parentId)}` : '';
  return api<StorageItem[]>(`/storage${params}`);
}

export async function createFolder(req: CreateFolderRequest): Promise<StorageItem> {
  return api<StorageItem>('/storage/folders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(req),
  });
}

export async function uploadFile(file: File, parentId?: string | null): Promise<StorageItem> {
  const form = new FormData();
  form.append('file', file);
  if (parentId) {
    form.append('parentId', parentId);
  }

  return api<StorageItem>('/storage/files', {
    method: 'POST',
    body: form,
  });
}

export async function deleteItem(id: string): Promise<void> {
  await api<void>(`/storage/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}

export async function getDownloadUrls(id: string): Promise<DownloadResponse> {
  return api<DownloadResponse>(`/storage/${encodeURIComponent(id)}/download`);
}
