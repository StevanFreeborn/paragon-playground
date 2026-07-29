<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { me, logout, type UserResponse } from '../services/auth';

const router = useRouter();
const user = ref<UserResponse | null>(null);

onMounted(async () => {
  user.value = await me();
});

async function handleLogout() {
  await logout();
  router.push('/login');
}
</script>

<template>
  <div class="dashboard">
    <header>
      <h1>Paragon Playground</h1>
      <button class="logout" @click="handleLogout">Sign out</button>
    </header>

    <main v-if="user">
      <section class="card">
        <h2>Welcome, {{ user.displayName }}</h2>
        <dl>
          <dt>Email</dt>
          <dd>{{ user.email }}</dd>
          <dt>Organization</dt>
          <dd>{{ user.organizationName }} ({{ user.organizationSlug }})</dd>
        </dl>
      </section>

      <section class="card">
        <h2>Next Steps</h2>
        <p>This harness is for Paragon integration exploration.</p>
      </section>
    </main>
  </div>
</template>

<style scoped>
.dashboard {
  max-width: 800px;
  margin: 0 auto;
  padding: 1rem;
}

header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

header h1 {
  font-size: 1.25rem;
}

.logout {
  padding: 0.5rem 1rem;
  background: none;
  border: 1px solid #ccc;
  border-radius: 4px;
  cursor: pointer;
}

.card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.1);
  margin-bottom: 1rem;
}

.card h2 {
  margin: 0 0 1rem;
  font-size: 1.1rem;
}

dl dt {
  font-weight: 600;
  margin-top: 0.5rem;
  color: #555;
}

dl dd {
  margin: 0 0 0.5rem;
}

code {
  background: #f0f0f0;
  padding: 0.1rem 0.3rem;
  border-radius: 3px;
  font-size: 0.9rem;
}

ul {
  padding-left: 1.25rem;
}

li {
  margin-bottom: 0.5rem;
}
</style>
