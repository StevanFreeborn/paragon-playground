<script setup lang="ts">
  import { ref, computed, onMounted } from 'vue';
  import type { IntegrationInstallEvent } from '@useparagon/connect';
  import AppIcon from '../components/AppIcon.vue';
  import {
    getParagonToken,
    getConfig,
    getCredentials,
    saveCredential,
    deleteCredential,
    type IntegrationConfig,
    type CredentialResponse,
  } from '../services/integration';
  import { formatLocaleDateWithTime } from '../utils/utils';

  const config = ref<IntegrationConfig | null>(null);
  const credentials = ref<CredentialResponse[]>([]);

  const loading = ref(true);
  const managing = ref(false);
  const connecting = ref(false);
  const disconnecting = ref(false);

  const error = ref('');
  const success = ref('');

  const spCredential = computed(() =>
    credentials.value.find((c) => c.integrationType === 'sharepoint'),
  );

  const integrationConfigured = computed(() => Boolean(config.value?.sharePointSiteId));

  const connectionMode = computed(() => config.value?.connectionMode ?? 'default');

  onMounted(() => {
    loadData();
  });

  async function loadData() {
    loading.value = true;
    error.value = '';

    try {
      const [cfg, creds] = await Promise.all([
        getConfig().catch(() => null),
        getCredentials().catch(() => []),
      ]);

      config.value = cfg;
      credentials.value = creds;
    } catch {
      error.value = 'Failed to load settings data';
    } finally {
      loading.value = false;
    }
  }

  async function manageConnection(accountType?: string[]) {
    managing.value = true;
    error.value = '';

    try {
      const tokenResponse = await getParagonToken();
      const { paragon } = await import('@useparagon/connect');
      await paragon.authenticate(tokenResponse.projectId, tokenResponse.paragonJwt);

      void paragon.connect('sharepoint', {
        selectedCredentialId: spCredential.value!.credentialId,
        ...(accountType ? { accountType } : {}),
        onClose: async () => {
          managing.value = false;
          await loadData();
        },
      });
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to open connection manager';
      managing.value = false;
    }
  }

  async function connectAccount(accountType?: string[]) {
    connecting.value = true;
    error.value = '';

    try {
      const tokenResponse = await getParagonToken();
      const { paragon, SDK_EVENT } = await import('@useparagon/connect');
      await paragon.authenticate(tokenResponse.projectId, tokenResponse.paragonJwt);

      const unsub = paragon.subscribe(
        SDK_EVENT.ON_INTEGRATION_INSTALL,
        async (event: IntegrationInstallEvent) => {
          if (!event.credentialId) {
            return;
          }

          try {
            await saveCredential({
              credentialId: event.credentialId,
              integrationType: 'sharepoint',
            });
            success.value = 'SharePoint account connected!';
            unsub();
            await loadData();
          } catch {
            error.value = 'Failed to save credential';
          }
        },
      );

      paragon.installIntegration('sharepoint', {
        allowMultipleCredentials: true,
        ...(accountType ? { accountType } : {}),
      });
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to connect account';
    } finally {
      connecting.value = false;
    }
  }

  async function disconnectAccount() {
    disconnecting.value = true;
    error.value = '';

    try {
      const tokenResponse = await getParagonToken();
      const { paragon } = await import('@useparagon/connect');
      await paragon.authenticate(tokenResponse.projectId, tokenResponse.paragonJwt);

      await paragon.uninstallIntegration('sharepoint', {
        selectedCredentialId: spCredential.value!.credentialId,
      });

      await deleteCredential(spCredential.value!.credentialId);
      success.value = 'SharePoint account disconnected.';
      await loadData();
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Failed to disconnect account';
    } finally {
      disconnecting.value = false;
    }
  }
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>Settings</h1>
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
        <h2>Integrations</h2>

        <template v-if="integrationConfigured">
          <div class="integration-row">
            <div class="integration-info">
              <span class="integration-icon"
                ><AppIcon
                  name="folder"
                  :size="20"
              /></span>
              <div>
                <h3 class="integration-name">SharePoint</h3>
                <p class="help-text integration-desc">
                  Connect your SharePoint account to upload and manage files.
                </p>
              </div>
            </div>

            <div class="connect-actions">
              <template v-if="spCredential">
                <button
                  class="btn btn-primary"
                  :disabled="managing"
                  @click="
                    manageConnection(
                      connectionMode === 'byo' ? ['user-configured-oauth'] : undefined,
                    )
                  "
                >
                  {{ managing ? 'Loading...' : 'Manage Connection' }}
                </button>

                <button
                  class="btn btn-danger"
                  :disabled="disconnecting"
                  @click="disconnectAccount()"
                >
                  {{ disconnecting ? 'Disconnecting...' : 'Disconnect Account' }}
                </button>
              </template>

              <button
                v-else
                class="btn btn-primary"
                :disabled="connecting"
                @click="
                  connectAccount(connectionMode === 'byo' ? ['user-configured-oauth'] : undefined)
                "
              >
                {{ connecting ? 'Connecting...' : 'Connect Account' }}
              </button>
            </div>
          </div>

          <div
            v-if="credentials.length > 0"
            class="credentials-list"
          >
            <h3>Your Connected Accounts</h3>
            <div
              v-for="cred in credentials"
              :key="cred.id"
              class="credential-row"
            >
              <span class="cred-type">{{ cred.integrationType }}</span>
              <span class="cred-date"
                >Connected {{ formatLocaleDateWithTime(cred.connectedAt) }}</span
              >
              <span class="cred-id">{{ cred.credentialId.slice(0, 12) }}...</span>
            </div>
          </div>
        </template>

        <template v-else>
          <p class="help-text text-muted">
            No integrations configured by your organization's admin yet.
          </p>
        </template>
      </section>
    </template>
  </div>
</template>

<style scoped>
  .integration-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1rem;
    flex-wrap: wrap;
  }

  .integration-info {
    display: flex;
    align-items: center;
    gap: 0.75rem;
  }

  .integration-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 2.25rem;
    height: 2.25rem;
    border: 1px solid var(--color-border);
    border-radius: var(--radius);
    color: var(--color-accent);
    flex-shrink: 0;
  }

  .integration-name {
    margin: 0;
  }

  .integration-desc {
    margin: 0.25rem 0 0;
  }

  .connect-actions {
    display: flex;
    gap: 0.75rem;
    flex-wrap: wrap;
  }

  .credentials-list {
    margin-top: 1.25rem;
    border-top: 1px solid var(--color-border);
  }

  .credential-row {
    display: flex;
    gap: 1rem;
    align-items: center;
    padding: 0.5rem 0;
    font-size: var(--text-sm);
  }

  .cred-type {
    font-weight: 600;
    text-transform: capitalize;
    min-width: 6rem;
  }

  .cred-date {
    color: var(--color-text-secondary);
  }

  .cred-id {
    color: var(--color-text-muted);
    font-family: ui-monospace, 'Cascadia Code', Consolas, monospace;
  }
</style>
