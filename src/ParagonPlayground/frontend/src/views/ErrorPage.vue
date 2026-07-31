<script setup lang="ts">
  import { useRouter } from 'vue-router';

  defineProps<{
    status: string;
    heading: string;
    title: string;
    message: string;
  }>();

  const router = useRouter();
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ heading }}</h1>
    </header>

    <section class="section">
      <p class="status-eyebrow">{{ status }}</p>
      <h2>{{ title }}</h2>
      <p class="help-text">{{ message }}</p>
      <button
        class="btn btn-primary"
        @click="router.push('/')"
      >
        Back to Dashboard
      </button>
    </section>
  </div>
</template>

<style scoped>
  .status-eyebrow {
    margin: 0 0 0.25rem;
    font-size: var(--text-xs);
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--color-accent);
  }

  h2 {
    margin: 0 0 0.75rem;
  }
</style>
