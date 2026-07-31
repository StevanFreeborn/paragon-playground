<script setup lang="ts">
  import { ref, computed, onMounted } from 'vue';
  import type { IntegrationInstallEvent } from '@useparagon/connect';
  import {
    getParagonToken,
    getConfig,
    updateConfig,
    getCredentials,
    saveCredential,
    purgeOrgCredentials,
    type IntegrationConfig,
    type IntegrationConfigRequest,
    type CredentialResponse,
  } from '../services/integration';
import { formatLocaleDateWithTime } from '../utils/utils';

  const config = ref<IntegrationConfig | null>(null);
  const credentials = ref<CredentialResponse[]>([]);

  const loading = ref(true);
  const saving = ref(false);
  const uninstalling = ref(false);

  const error = ref('');
  const success = ref('');

  const form = ref<IntegrationConfigRequest>({
    connectionMode: 'default',
    sharePointSiteUrl: null,
    sharePointFolderPath: null,
  });

  const hasOrgIntegration = computed(() =>
    credentials.value.some((c) => c.integrationType === 'sharepoint'),
  );

  const submitLabel = computed(() => {
    if (saving.value) {
      return 'Saving...';
    }

    return hasOrgIntegration.value ? 'Save Configuration' : 'Save & Set Up Integration';
  });

  onMounted(() => {
    loadData();
  });

  async function loadData() {
    loading.value = true;
    error.value = '';

    try {
      const [cfg, myCreds] = await Promise.all([
        getConfig().catch(() => null),
        getCredentials().catch(() => []),
      ]);

      config.value = cfg;
      credentials.value = myCreds;

      if (cfg) {
        form.value = {
          connectionMode: cfg.connectionMode,
          sharePointSiteUrl: cfg.sharePointSiteUrl,
          sharePointFolderPath: cfg.sharePointFolderPath,
        };
      }
    } catch {
      error.value = 'Failed to load integration data';
    } finally {
      loading.value = false;
    }
  }

  async function saveConfig() {
    if (!form.value.sharePointSiteUrl?.trim()) {
      error.value = 'SharePoint Site URL is required to set up the integration.';
      return;
    }

    saving.value = true;
    error.value = '';

    try {
      if (hasOrgIntegration.value) {
        const updated = await updateConfig(form.value);

        config.value = updated;
        success.value = 'Configuration saved';
      } else {
        await installAndSave();
      }
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to save configuration';
    } finally {
      saving.value = false;
    }
  }

  async function installAndSave() {
    const tokenResponse = await getParagonToken();
    const { paragon, SDK_EVENT } = await import('@useparagon/connect');
    await paragon.authenticate(tokenResponse.projectId, tokenResponse.paragonJwt);

    const unsubscribeInstall = paragon.subscribe(
      SDK_EVENT.ON_INTEGRATION_INSTALL,
      async (event: IntegrationInstallEvent) => {
        if (!event.credentialId) {
          return;
        }

        unsubscribeInstall();
        saving.value = true;

        try {
          await saveCredential({
            credentialId: event.credentialId,
            integrationType: 'sharepoint',
          });

          const updated = await updateConfig(form.value);

          config.value = updated;
          success.value = 'SharePoint integration installed and configured!';

          await loadData();
        } catch (e: unknown) {
          error.value = e instanceof Error ? e.message : 'Failed to complete setup';
        } finally {
          saving.value = false;
        }
      },
    );

    try {
      paragon.installIntegration('sharepoint', {
        allowMultipleCredentials: true,
        ...(form.value.connectionMode === 'byo' ? { accountType: ['user-configured-oauth'] } : {}),
      });
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to start setup';
      unsubscribeInstall();
    }
  }

  async function uninstallOrgIntegration() {
    uninstalling.value = true;
    error.value = '';

    try {
      const tokenResponse = await getParagonToken();
      const { paragon } = await import('@useparagon/connect');
      await paragon.authenticate(tokenResponse.projectId, tokenResponse.paragonJwt);

      await paragon.uninstallIntegration('sharepoint');
      await purgeOrgCredentials();

      success.value = 'SharePoint integration removed from organization.';

      await loadData();
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to uninstall integration';
    } finally {
      uninstalling.value = false;
    }
  }
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>Integrations</h1>
    </header>

    <div
      v-if="error"
      class="alert error"
    >
      {{ error }}
    </div>
    <div
      v-if="success"
      class="alert success"
    >
      {{ success }}
    </div>

    <div
      v-if="loading"
      class="loading"
    >
      Loading...
    </div>

    <template v-if="!loading">
      <section class="section">
        <h2>Integration Configuration</h2>
        <p class="help-text">
          Choose how users authenticate to Microsoft and where files are stored. First-time setup
          connects the integration before saving.
        </p>

        <form
          class="config-form"
          @submit.prevent="saveConfig"
        >
          <label>
            <span class="label-title">Connection Mode</span>
            <select v-model="form.connectionMode">
              <option value="default">Default — ISV-provided Azure AD app</option>
              <option value="byo">
                User-Configured OAuth — organization provides their own Azure AD app
              </option>
            </select>
            <span class="field-note">
              Default uses our Azure AD app; BYO uses the organization's own.
            </span>
          </label>

          <label>
            <span class="label-title">SharePoint Site URL</span>
            <input
              v-model="form.sharePointSiteUrl"
              type="url"
              placeholder="https://contoso.sharepoint.com/sites/MySite"
            />
            <span class="field-note"
              >Site ID is resolved from this URL using the connected admin account, which must have
              access to the site.</span
            >
          </label>

          <label>
            <span class="label-title">SharePoint Folder Path</span>
            <input
              v-model="form.sharePointFolderPath"
              type="text"
              placeholder="e.g. Uploads/MyApp"
            />
            <span class="field-note">Created automatically if it doesn't exist.</span>
          </label>

          <div class="connect-actions">
            <button
              class="btn btn-primary"
              type="submit"
              :disabled="saving || uninstalling"
            >
              {{ submitLabel }}
            </button>

            <button
              v-if="hasOrgIntegration"
              class="btn btn-danger"
              type="button"
              :disabled="saving || uninstalling"
              @click="uninstallOrgIntegration()"
            >
              {{ uninstalling ? 'Uninstalling...' : 'Uninstall Organization Integration' }}
            </button>
          </div>
        </form>

        <div
          v-if="config"
          class="config-meta"
        >
          Last updated: {{ formatLocaleDateWithTime(config.updatedAt) }}
        </div>
      </section>

      <p class="help-text text-muted">
        Users connect and manage their own accounts from the Settings page.
      </p>
    </template>
  </div>
</template>

<style scoped>
  .connect-actions {
    display: flex;
    gap: 0.75rem;
    flex-wrap: wrap;
    margin-top: 1rem;
  }

  .config-form label {
    display: block;
    margin-bottom: 0.75rem;
    font-weight: 600;
    font-size: var(--text-sm);
  }

  .label-title {
    display: block;
    margin-bottom: 0.25rem;
  }

  .config-form input,
  .config-form select {
    display: block;
    width: 100%;
    margin-top: 0.25rem;
  }

  .field-note {
    display: block;
    margin-top: 0.25rem;
    font-weight: 400;
    font-size: var(--text-xs);
    color: var(--color-text-muted);
  }

  .config-meta {
    margin-top: 1rem;
    font-size: var(--text-xs);
    color: var(--color-text-muted);
  }
</style>
