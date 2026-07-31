<script setup lang="ts">
  import { ref, onMounted } from 'vue';
  import { me, type UserResponse } from '../services/auth';

  const user = ref<UserResponse | null>(null);

  onMounted(async () => {
    user.value = await me();
  });
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>Paragon Playground</h1>
    </header>

    <main v-if="user">
      <section class="section">
        <h2>Welcome, {{ user.displayName }}</h2>
        <dl>
          <dt>Email</dt>
          <dd>{{ user.email }}</dd>
          <dt>Organization</dt>
          <dd>{{ user.organizationName }} ({{ user.organizationSlug }})</dd>
          <dt>Role</dt>
          <dd>{{ user.role }}</dd>
        </dl>
      </section>

      <section class="section">
        <h2>Examples</h2>
        <div class="actions">
          <router-link
            to="/files"
            class="action-card"
          >
            <h3>File Explorer</h3>
            <p>Browse, upload, and manage files</p>
          </router-link>
        </div>
      </section>
    </main>
  </div>
</template>

<style scoped>
  .actions {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
    gap: 0.75rem;
    margin-top: 0.75rem;
  }

  .action-card {
    display: block;
    text-decoration: none;
    color: inherit;
    border: 1px solid var(--color-border);
    border-radius: var(--radius);
    padding: 1rem 1.125rem;
    transition:
      border-color 0.12s,
      background-color 0.12s;
  }

  .action-card:hover {
    border-color: var(--color-accent);
    background: var(--color-surface-subtle);
  }

  .action-card h3 {
    margin: 0 0 0.25rem;
  }

  .action-card p {
    margin: 0;
    font-size: var(--text-sm);
    color: var(--color-text-secondary);
  }

  dl dt {
    font-weight: 600;
    margin-top: 0.5rem;
    color: var(--color-text-secondary);
    font-size: var(--text-sm);
  }

  dl dd {
    margin: 0 0 0.5rem;
    font-size: var(--text-sm);
  }
</style>
