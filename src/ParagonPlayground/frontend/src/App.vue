<script setup lang="ts">
  import { computed, onMounted } from 'vue';
  import { useRoute, useRouter } from 'vue-router';
  import { useCurrentUser } from './composables/useCurrentUser';
  import { logout } from './services/auth';

  const route = useRoute();
  const router = useRouter();
  const { currentUser, fetchCurrentUser, clearCurrentUser } = useCurrentUser();

  const showNav = computed(() => route.meta.public !== true);

  const isActive = (name: string) => route.name === name;

  onMounted(async () => {
    if (!currentUser.value) {
      await fetchCurrentUser();
    }
  });

  async function handleLogout() {
    await logout();
    clearCurrentUser();
    router.push('/login');
  }
</script>

<template>
  <div class="app-shell">
    <nav
      v-if="showNav"
      class="app-nav"
    >
      <span class="app-brand">Paragon Playground</span>
      <div class="nav-links">
        <router-link
          to="/"
          :class="{ active: isActive('dashboard') }"
        >
          Dashboard
        </router-link>
        <router-link
          to="/settings"
          :class="{ active: isActive('settings') }"
        >
          Settings
        </router-link>
        <router-link
          v-if="currentUser?.role === 'admin'"
          to="/integrations"
          :class="{ active: isActive('integrations') }"
        >
          Integrations
        </router-link>
      </div>
      <button
        class="btn"
        @click="handleLogout"
      >
        Sign out
      </button>
    </nav>

    <router-view />
  </div>
</template>

<style scoped>
  .app-nav {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 1rem;
    height: 3.25rem;
    padding: 0 1rem;
    border-bottom: 1px solid var(--color-border);
    background: var(--color-bg);
  }

  .app-brand {
    font-weight: 650;
    font-size: var(--text-sm);
    letter-spacing: 0.01em;
    color: var(--color-accent-strong);
  }

  .app-nav .nav-links {
    height: 100%;
    gap: 1.25rem;
  }

  .app-nav .nav-links a {
    display: inline-flex;
    align-items: center;
    height: 100%;
    padding: 0 0.125rem;
    border-bottom: 2px solid transparent;
    color: var(--color-text-secondary);
    transition: color 0.12s;
  }

  .app-nav .nav-links a:hover {
    color: var(--color-text);
  }

  .app-nav .nav-links a.active {
    color: var(--color-accent);
    border-bottom-color: var(--color-accent);
    font-weight: 600;
  }
</style>
