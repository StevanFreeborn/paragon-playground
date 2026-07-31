<script setup lang="ts">
  import { ref } from 'vue';
  import { useRouter } from 'vue-router';
  import { login } from '../services/auth';

  const router = useRouter();
  const email = ref('');
  const password = ref('');
  const error = ref('');
  const loading = ref(false);

  async function handleSubmit() {
    error.value = '';
    loading.value = true;

    try {
      await login(email.value, password.value);
      router.push('/');
    } catch (e: unknown) {
      error.value = e instanceof Error ? e.message : 'Login failed';
    } finally {
      loading.value = false;
    }
  }
</script>

<template>
  <div class="login-container">
    <form
      class="login-form"
      @submit.prevent="handleSubmit"
    >
      <h1>Paragon Playground</h1>
      <p class="subtitle">Sign in to your account</p>

      <div
        v-if="error"
        class="alert error"
      >
        {{ error }}
      </div>

      <label>
        Email
        <input
          v-model="email"
          type="email"
          required
          autocomplete="email"
        />
      </label>

      <label>
        Password
        <input
          v-model="password"
          type="password"
          required
          autocomplete="current-password"
        />
      </label>

      <button
        class="btn btn-primary login-submit"
        type="submit"
        :disabled="loading"
      >
        {{ loading ? 'Signing in...' : 'Sign in' }}
      </button>
    </form>
  </div>
</template>

<style scoped>
  .login-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    padding: 1rem;
  }

  .login-form {
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius);
    padding: 2rem;
    width: 100%;
    max-width: 400px;
  }

  .login-form h1 {
    margin: 0 0 0.25rem;
    font-size: var(--text-xl);
    font-weight: 650;
  }

  .subtitle {
    color: var(--color-text-secondary);
    font-size: var(--text-sm);
    margin-bottom: 1.5rem;
  }

  label {
    display: block;
    margin-bottom: 1rem;
    font-weight: 600;
    font-size: var(--text-sm);
  }

  input {
    display: block;
    width: 100%;
    margin-top: 0.25rem;
  }

  .login-submit {
    width: 100%;
    padding: 0.625rem;
  }
</style>
