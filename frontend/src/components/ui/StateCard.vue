<script setup lang="ts">
withDefaults(
  defineProps<{
    message: string
    type?: 'default' | 'error'
  }>(),
  { type: 'default' },
)
</script>

<template>
  <section class="state-card" :class="type">{{ message }}</section>
</template>

<style scoped>
.state-card {
  padding: 24px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
  color: var(--tj-text);
}

.state-card.error {
  color: var(--tj-danger);
}
</style>
