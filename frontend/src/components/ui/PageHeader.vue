<script setup lang="ts">
defineProps<{
  title: string
  subtitle?: string
  eyebrow?: string
}>()
</script>

<template>
  <header class="page-header">
    <div>
      <p v-if="eyebrow" class="eyebrow">{{ eyebrow }}</p>
      <h1>{{ title }}</h1>
      <p v-if="subtitle" class="subtitle">{{ subtitle }}</p>
    </div>
    <div v-if="$slots.actions" class="actions">
      <slot name="actions" />
    </div>
  </header>
</template>

<style scoped>
.page-header {
  display: flex;
  justify-content: space-between;
  gap: 20px;
  align-items: flex-start;
  margin-bottom: 24px;
}

.eyebrow {
  margin: 0 0 8px;
  font-size: 12px;
  letter-spacing: 0.22em;
  text-transform: uppercase;
  color: #4d77ff;
}

.page-header h1 {
  margin: 0;
  font-size: 32px;
  color: var(--tj-text);
}

.subtitle {
  margin: 12px 0 0;
  max-width: 720px;
  color: var(--tj-text-muted);
  line-height: 1.7;
}

.actions {
  display: flex;
  gap: 12px;
  align-items: center;
}
</style>
