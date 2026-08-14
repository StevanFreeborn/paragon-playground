<script setup lang="ts">
  import { ref, onMounted, computed, watch } from 'vue';
  import { useRoute, useRouter } from 'vue-router';
  import AppIcon from '../components/AppIcon.vue';
  import {
    getItems,
    createFolder,
    uploadFile,
    deleteItem,
    getDownloadUrls,
    type StorageItem,
    createSyncedFolder,
  } from '../services/storage';
  import { formatLocaleDate } from '../utils/utils';
  import { getParagonToken } from '../services/integration';

  type SharePointIds = {
    listId: string;
    webId: string;
    siteId: string;
    listItemId: string;
    listItemUniqueId: string;
  };

  type ParentReference = {
    driveId: string;
    sharepointIds: SharePointIds;
  };

  type SelectedFilePickerItem = {
    id: string;
    parentReference: ParentReference;
    sharepointIds: SharePointIds;
  };

  type ParagonPickerInstance = {
    init: () => Promise<void>;
    open: () => void;
  };

  type ParagonSDKWithPicker = {
    authenticate: (projectId: string, jwt: string) => Promise<void>;
    ExternalFilePicker: new (
      integration: string,
      options: {
        onFileSelect: (items: SelectedFilePickerItem[]) => Promise<void> | void;
      },
    ) => ParagonPickerInstance;
  };

  const route = useRoute();
  const router = useRouter();

  const items = ref<StorageItem[]>([]);
  const loading = ref(true);
  const error = ref('');
  const breadcrumbs = ref<{ id: string | null; name: string }[]>([]);
  const showNewFolder = ref(false);
  const newFolderName = ref('');
  const currentFolder = ref<StorageItem | null>(null);

  const folders = computed(() => items.value.filter((i) => i.isFolder));
  const files = computed(() => items.value.filter((i) => !i.isFolder));
  const isCurrentFolderReadOnly = computed(() => currentFolder.value?.isReadOnly ?? false);

  const pathSegments = computed(() => {
    const raw = route.params.pathMatch;

    if (!raw) {
      return [];
    }

    return (Array.isArray(raw) ? raw : [raw]).filter(Boolean);
  });

  onMounted(() => {
    navigateToPath(pathSegments.value);
  });

  watch(pathSegments, (newPath) => {
    navigateToPath(newPath);
  });

  async function navigateToPath(segments: string[]) {
    loading.value = true;
    error.value = '';

    try {
      let parentId: string | null = null;
      const crumbs: { id: string | null; name: string }[] = [];
      let resolvedFolder: StorageItem | null = null;

      for (const name of segments) {
        const children = await getItems(parentId);
        const folder = children.find((i) => i.isFolder && i.name === name);

        if (!folder) {
          error.value = `Folder "${name}" not found`;
          items.value = [];
          return;
        }

        crumbs.push({ id: folder.id, name: folder.name });
        parentId = folder.id;
        resolvedFolder = folder;
      }

      currentFolder.value = resolvedFolder;
      breadcrumbs.value = crumbs;

      items.value = await getItems(currentFolder.value?.id ?? null);
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to navigate';
      items.value = [];
      currentFolder.value = null;
    } finally {
      loading.value = false;
    }
  }

  function openFolder(folder: StorageItem) {
    router.push({ params: { pathMatch: [...pathSegments.value, folder.name] } });
  }

  function navigateToBreadcrumb(index: number) {
    router.push({ params: { pathMatch: pathSegments.value.slice(0, index) } });
  }

  async function handleCreateFolder() {
    if (!newFolderName.value.trim()) {
      return;
    }

    try {
      await createFolder({
        name: newFolderName.value.trim(),
        parentId: currentFolder.value?.id ?? null,
      });

      newFolderName.value = '';
      showNewFolder.value = false;

      await navigateToPath(pathSegments.value);
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to create folder';
    }
  }

  async function handleUpload(e: Event) {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    try {
      await uploadFile(file, currentFolder.value?.id ?? null);

      input.value = '';

      await navigateToPath(pathSegments.value);
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to upload file';
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this item? The file will remain in SharePoint.')) {
      return;
    }

    try {
      await deleteItem(id);
      await navigateToPath(pathSegments.value);
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to delete item';
    }
  }

  async function handleDownload(id: string) {
    try {
      const urls = await getDownloadUrls(id);

      if (urls.sharePointUrl) {
        open(urls.sharePointUrl, '_blank');
      }
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to get download URL';
    }
  }

  async function handleProxyDownload(id: string) {
    try {
      const urls = await getDownloadUrls(id);

      if (urls.proxyUrl) {
        open(urls.proxyUrl, '_blank');
      }
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to get download URL';
    }
  }

  function formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1048576).toFixed(1)} MB`;
  }

  async function openSharePointFolderPicker() {
    try {
      const tokenResponse = await getParagonToken();
      const { paragon } = await import('@useparagon/connect');
      const sdk = paragon as unknown as ParagonSDKWithPicker;

      await sdk.authenticate(tokenResponse.projectId, tokenResponse.paragonJwt);

      const picker = new sdk.ExternalFilePicker('sharepoint', {
        onFileSelect: async (selectedItems: SelectedFilePickerItem[]) => {
          if (!selectedItems || selectedItems.length === 0) return;

          const selectedItem = selectedItems[0];
          const folderId = selectedItem.id;
          const siteId = selectedItem.sharepointIds.siteId;

          await createSyncedFolder({
            sharePointFolderId: folderId,
            sharePointSiteId: siteId,
            parentId: currentFolder.value?.id ?? null,
          });

          await navigateToPath(pathSegments.value);
        },
      });

      await picker.init();
      picker.open();
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to open SharePoint picker';
    }
  }
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>Files</h1>
    </header>

    <div
      v-if="error"
      class="alert error"
    >
      {{ error }}
    </div>

    <nav class="breadcrumbs">
      <button
        class="crumb"
        :class="{ active: currentFolder === null }"
        @click="navigateToBreadcrumb(0)"
      >
        Root
      </button>
      <template
        v-for="(crumb, i) in breadcrumbs"
        :key="i"
      >
        <span class="crumb-sep">/</span>
        <button
          class="crumb"
          :class="{ active: i === breadcrumbs.length - 1 }"
          @click="navigateToBreadcrumb(i + 1)"
        >
          {{ crumb.name }}
        </button>
      </template>
    </nav>

    <div class="toolbar">
      <button
        class="btn"
        :disabled="isCurrentFolderReadOnly"
        @click="showNewFolder = !showNewFolder"
      >
        <AppIcon
          v-if="!showNewFolder"
          name="plus"
          :size="14"
        />
        {{ showNewFolder ? 'Cancel' : 'New Folder' }}
      </button>

      <label
        class="btn btn-primary upload-label"
        :class="{ disabled: isCurrentFolderReadOnly }"
      >
        <AppIcon
          name="upload"
          :size="14"
        />
        Upload File
        <input
          type="file"
          hidden
          :disabled="isCurrentFolderReadOnly"
          @change="handleUpload"
        />
      </label>

      <button
        class="btn btn-secondary"
        @click="openSharePointFolderPicker"
      >
        <AppIcon
          name="sharepoint"
          :size="14"
        />
        Sync Folder from SharePoint
      </button>
    </div>

    <div
      v-if="showNewFolder"
      class="inline-form"
    >
      <input
        v-model="newFolderName"
        type="text"
        placeholder="Folder name"
        @keyup.enter="handleCreateFolder"
      />
      <button
        class="btn btn-primary"
        @click="handleCreateFolder"
      >
        Create
      </button>
    </div>

    <div
      v-if="loading"
      class="loading"
    >
      Loading...
    </div>

    <div
      v-else-if="items.length === 0"
      class="empty"
    >
      <p>This folder is empty.</p>
    </div>

    <div
      v-else
      class="item-list"
    >
      <div
        v-for="folder in folders"
        :key="folder.id"
        class="item-row folder"
        @dblclick="openFolder(folder)"
      >
        <span class="item-icon"
          ><AppIcon
            name="folder"
            :size="16"
        /></span>
        <span
          class="item-name"
          @click="openFolder(folder)"
          >{{ folder.name }}</span
        >
        <span class="item-meta">Folder</span>
        <span class="item-user">{{ folder.createdByDisplayName }}</span>
        <span class="item-date">{{ formatLocaleDate(folder.createdAt) }}</span>
        <span class="item-actions">
          <button
            v-if="!folder.isReadOnly || folder.isManagedSync"
            class="btn-small btn-small-danger"
            title="Delete"
            @click="handleDelete(folder.id)"
          >
            <AppIcon
              name="trash"
              :size="14"
            />
          </button>
        </span>
      </div>

      <div
        v-for="file in files"
        :key="file.id"
        class="item-row file"
      >
        <span class="item-icon"
          ><AppIcon
            name="file"
            :size="16"
        /></span>
        <span class="item-name">{{ file.name }}</span>
        <span class="item-meta">{{ formatSize(file.fileSize) }}</span>
        <span class="item-user">{{ file.createdByDisplayName }}</span>
        <span class="item-date">{{ formatLocaleDate(file.createdAt) }}</span>
        <span class="item-actions">
          <button
            class="btn-small"
            title="Open in SharePoint"
            @click="handleDownload(file.id)"
          >
            <AppIcon
              name="external"
              :size="14"
            />
          </button>
          <button
            class="btn-small"
            title="Download via app"
            @click="handleProxyDownload(file.id)"
          >
            <AppIcon
              name="download"
              :size="14"
            />
          </button>
          <button
            v-if="!file.isReadOnly"
            class="btn-small btn-small-danger"
            title="Delete"
            @click="handleDelete(file.id)"
          >
            <AppIcon
              name="trash"
              :size="14"
            />
          </button>
        </span>
      </div>
    </div>
  </div>
</template>

<style scoped>
  .page {
    max-width: 960px;
  }

  .page-header {
    margin-bottom: 1rem;
  }

  .breadcrumbs {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    margin-bottom: 1rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid var(--color-border);
  }

  .crumb {
    background: none;
    border: none;
    padding: 0.25rem 0.5rem;
    border-radius: var(--radius);
    cursor: pointer;
    font-size: var(--text-sm);
    color: var(--color-accent);
  }

  .crumb.active {
    font-weight: 600;
    color: var(--color-text);
    cursor: default;
  }

  .crumb-sep {
    color: var(--color-text-muted);
    font-size: var(--text-sm);
  }

  .toolbar {
    display: flex;
    gap: 0.5rem;
    margin-bottom: 1rem;
  }

  .upload-label {
    cursor: pointer;
  }

  .inline-form {
    display: flex;
    gap: 0.5rem;
    margin-bottom: 1rem;
    align-items: center;
  }

  .inline-form input {
    flex: 1;
  }

  .item-list {
    border: 1px solid var(--color-border);
    overflow: hidden;
  }

  .item-row {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.5rem 0.75rem;
    border-bottom: 1px solid var(--color-border);
    font-size: var(--text-sm);
  }

  .item-row:last-child {
    border-bottom: none;
  }

  .item-row.folder {
    cursor: pointer;
  }

  .item-row.folder:hover {
    background: var(--color-surface-subtle);
  }

  .item-icon {
    display: inline-flex;
    align-items: center;
    flex-shrink: 0;
    color: var(--color-text-muted);
  }

  .folder .item-icon {
    color: var(--color-accent);
  }

  .item-name {
    flex: 1;
    font-weight: 500;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .folder .item-name {
    color: var(--color-accent);
  }

  .item-meta {
    width: 5rem;
    text-align: right;
    color: var(--color-text-secondary);
    flex-shrink: 0;
  }

  .item-user {
    width: 8rem;
    color: var(--color-text-secondary);
    flex-shrink: 0;
  }

  .item-date {
    width: 7rem;
    color: var(--color-text-muted);
    flex-shrink: 0;
  }

  .item-actions {
    display: flex;
    gap: 0.25rem;
    flex-shrink: 0;
    width: 6.5rem;
    justify-content: flex-end;
  }
</style>
